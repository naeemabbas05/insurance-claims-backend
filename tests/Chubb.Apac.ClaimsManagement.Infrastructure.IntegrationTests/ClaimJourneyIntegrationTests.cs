using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Chubb.Apac.ClaimsManagement.Infrastructure.Integration;
using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.IntegrationTests;

public sealed class ClaimJourneyIntegrationTests
{
    private const string ConnectionStringEnvironmentVariable =
        "CHUBB_CLAIMS_SQLSERVER_TEST_CONNECTION_STRING";

    private static readonly DateTimeOffset Now = Utc(2026, 11, 21);

    [SqlServerIntegrationFact]
    public async Task ClaimJourneyPersistsLifecycleAssignmentsAndCurrencyScopedExposure()
    {
        var connectionString = CreateIsolatedDatabaseConnectionString();
        var options = new DbContextOptionsBuilder<ClaimsDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        await using var dbContext = new ClaimsDbContext(options);
        try
        {
            await dbContext.Database.MigrateAsync();
            var marketCatalog = MarketCatalog.Create(await dbContext.Markets.ToArrayAsync());
            var handler = CreateStaff(StaffRole.ClaimsHandler, marketCatalog);
            var adjuster = CreateStaff(StaffRole.Adjuster, marketCatalog);
            var assessor = CreateStaff(StaffRole.Assessor, marketCatalog);
            var approver = CreateStaff(StaffRole.Approver, marketCatalog);
            var financeOfficer = CreateStaff(StaffRole.FinanceOfficer, marketCatalog);
            var policy = Policy.Create(
                PolicyNumber.Create($"POL-IT-{Guid.NewGuid():N}"),
                marketCatalog,
                MarketCode.Create("MY"),
                PolicyType.Motor,
                PersonName.Create("Integration Test Holder"),
                DateRange.Create(Utc(2026, 1, 1), Utc(2027, 1, 1)),
                Money.Create(50_000m, "MYR"),
                Utc(2025, 12, 1));
            dbContext.Policies.Add(policy);
            dbContext.StaffUsers.AddRange(handler, adjuster, assessor, approver, financeOfficer);
            await dbContext.SaveChangesAsync();

            var clock = new FixedTimeProvider(Now);
            var eventOutbox = new EfCoreClaimIntegrationEventOutbox(dbContext);
            var submissionService = new ClaimSubmissionService(
                new ClaimSubmissionRepository(dbContext),
                new SubmitClaimCommandValidator(clock),
                eventOutbox,
                clock);
            var lifecycleService = CreateLifecycleService(dbContext, eventOutbox, clock);
            var staffService = CreateStaffService(dbContext, eventOutbox, clock);
            var reportingService = new ManagementClaimsReportingService(
                new ManagementClaimsReportRepository(dbContext),
                new ManagementClaimsReportValidator());
            var submissionCommand = new SubmitClaimCommand(
                $"journey-{Guid.NewGuid():N}",
                "MY",
                policy.Number.Value,
                ClaimType.Motor,
                Utc(2026, 11, 20),
                "Integration test motor loss.",
                "Integration Test Holder",
                "claimant@example.com",
                "+60123456789",
                1_200m,
                "MYR");

            var submitted = await submissionService.SubmitAsync(
                submissionCommand,
                CancellationToken.None);
            var replay = await submissionService.SubmitAsync(
                submissionCommand,
                CancellationToken.None);
            Assert.Equal(ClaimStatus.Submitted, submitted.Status);
            Assert.True(replay.IsReplay);
            Assert.Equal(submitted.ClaimReference, replay.ClaimReference);

            var assignment = await staffService.AssignAsync(
                new AssignClaimCommand(
                    handler.Id,
                    submitted.ClaimReference,
                    adjuster.Id,
                    AssignmentRole.Adjuster),
                CancellationToken.None);
            Assert.Equal(adjuster.Id, assignment.OfficerId);

            await lifecycleService.ChangeWorkflowStatusAsync(
                new ChangeClaimWorkflowStatusCommand(
                    submitted.ClaimReference,
                    handler.Id,
                    ClaimStatus.UnderReview,
                    "Integration review started."),
                CancellationToken.None);
            await lifecycleService.RecordAssessmentAsync(
                new RecordClaimAssessmentCommand(
                    submitted.ClaimReference,
                    assessor.Id,
                    1_000m,
                    800m,
                    "MYR",
                    "Integration assessment."),
                CancellationToken.None);
            var approval = await lifecycleService.ApproveAsync(
                new ApproveClaimCommand(
                    submitted.ClaimReference,
                    approver.Id,
                    750m,
                    "MYR",
                    "Integration approval."),
                CancellationToken.None);
            var settlement = await lifecycleService.ProposeSettlementAsync(
                new ProposeClaimSettlementCommand(
                    submitted.ClaimReference,
                    handler.Id,
                    (await GetClaimAsync(dbContext, submitted.ClaimReference)).Claimants.Single().Id,
                    750m,
                    "MYR",
                    IsFinal: true),
                CancellationToken.None);
            await lifecycleService.ApproveSettlementAsync(
                new ApproveClaimSettlementCommand(
                    submitted.ClaimReference,
                    approver.Id,
                    settlement.ResourceId!.Value),
                CancellationToken.None);

            var reportBeforePayment = await reportingService.GetClaimsReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            Assert.Equal(750m, approval.ApprovedAmount);
            Assert.Equal([new CurrencyAmountTotal("MYR", 1_200m)], reportBeforePayment.OutstandingEstimatedExposure);
            Assert.Equal([new CurrencyAmountTotal("MYR", 1_000m)], reportBeforePayment.AssessedAmount);
            Assert.Equal([new CurrencyAmountTotal("MYR", 750m)], reportBeforePayment.ApprovedAmount);
            Assert.Equal([new CurrencyAmountTotal("MYR", 750m)], reportBeforePayment.ApprovedButUnpaidLiability);
            Assert.Empty(reportBeforePayment.SettledAmount);

            var paid = await lifecycleService.PaySettlementAsync(
                new PayClaimSettlementCommand(
                    submitted.ClaimReference,
                    financeOfficer.Id,
                    settlement.ResourceId.Value),
                CancellationToken.None);
            var finalReport = await reportingService.GetClaimsReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            Assert.Equal(ClaimStatus.Settled, paid.Status);
            Assert.Empty(finalReport.OutstandingEstimatedExposure);
            Assert.Empty(finalReport.ApprovedButUnpaidLiability);
            Assert.Equal([new CurrencyAmountTotal("MYR", 750m)], finalReport.SettledAmount);

            var storedClaim = await GetClaimAsync(dbContext, submitted.ClaimReference);
            Assert.Equal(ClaimStatus.Settled, storedClaim.Status);
            Assert.Contains(storedClaim.Assignments, item =>
                item.StaffUserId == adjuster.Id &&
                item.Role == AssignmentRole.Adjuster &&
                item.IsActive);
            Assert.Contains(storedClaim.AuditHistory, item => item.Action == "SettlementPaid");
            var outboxEventTypes = await dbContext.OutboxMessages
                .Select(item => item.EventType)
                .ToArrayAsync();
            Assert.Single(outboxEventTypes, eventType => eventType == "ClaimSubmitted");
            Assert.Contains("ClaimAssigned", outboxEventTypes);
            Assert.Contains("ClaimApproved", outboxEventTypes);
            Assert.Contains("ClaimSettled", outboxEventTypes);
        }
        finally
        {
            await dbContext.Database.EnsureDeletedAsync();
        }
    }

    private static ClaimLifecycleService CreateLifecycleService(
        ClaimsDbContext dbContext,
        IClaimIntegrationEventOutbox eventOutbox,
        TimeProvider clock) =>
        new(
            new ClaimLifecycleRepository(dbContext),
            new RecordClaimAssessmentCommandValidator(),
            new ApproveClaimCommandValidator(),
            new RejectClaimCommandValidator(),
            new RequestClaimInformationCommandValidator(clock),
            new RespondToClaimInformationCommandValidator(),
            new CancelClaimInformationCommandValidator(),
            new ProposeClaimSettlementCommandValidator(),
            new ApproveClaimSettlementCommandValidator(),
            new PayClaimSettlementCommandValidator(),
            new CancelClaimSettlementCommandValidator(),
            new ChangeClaimWorkflowStatusCommandValidator(),
            eventOutbox,
            clock);

    private static ClaimStaffOperationsService CreateStaffService(
        ClaimsDbContext dbContext,
        IClaimIntegrationEventOutbox eventOutbox,
        TimeProvider clock) =>
        new(
            new ClaimStaffOperationsRepository(dbContext),
            new ClaimMarketPageQueryValidator(),
            new OfficerClaimsPageQueryValidator(),
            new OfficerWorkloadQueryValidator(),
            new ClaimAssignmentHistoryQueryValidator(),
            new AssignClaimCommandValidator(),
            new ReassignClaimCommandValidator(),
            eventOutbox,
            clock);

    private static StaffUser CreateStaff(StaffRole role, MarketCatalog marketCatalog) =>
        StaffUser.Create(
            PersonName.Create($"{role} Integration Test"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            role,
            [MarketCode.Create("MY")],
            marketCatalog,
            Utc(2025, 1, 1));

    private static async Task<Claim> GetClaimAsync(ClaimsDbContext dbContext, string claimReference) =>
        await dbContext.Claims
            .Include(claim => claim.Claimants)
            .Include(claim => claim.Assignments)
            .Include(claim => claim.AuditHistory)
            .SingleAsync(claim => claim.Number == ClaimNumber.Create(claimReference));

    private static string CreateIsolatedDatabaseConnectionString()
    {
        var baseConnectionString =
            Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(baseConnectionString))
        {
            throw new InvalidOperationException(
                $"Set {ConnectionStringEnvironmentVariable} to a SQL Server connection string to run integration tests.");
        }

        var connection = new SqlConnectionStringBuilder(baseConnectionString)
        {
            InitialCatalog = $"ClaimsJourneyTests_{Guid.NewGuid():N}"
        };
        return connection.ConnectionString;
    }

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class SqlServerIntegrationFactAttribute : FactAttribute
    {
        public SqlServerIntegrationFactAttribute()
        {
            if (string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)))
            {
                Skip = $"Set {ConnectionStringEnvironmentVariable} to run SQL Server integration tests.";
            }
        }
    }
}
