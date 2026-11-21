using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;
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
                new ManagementClaimsReportValidator(),
                new ManagementReportingOptions(),
                clock);
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

            var agingReport = await reportingService.GetAgingReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            var performanceReport = await reportingService.GetPerformanceReportAsync(
                new ManagementClaimsReportQuery(
                    approver.Id,
                    "MY",
                    new DateOnly(2026, 11, 21),
                    new DateOnly(2026, 11, 21)),
                CancellationToken.None);
            var slaReport = await reportingService.GetSlaReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            Assert.Equal(1, agingReport.TotalOpenClaims);
            Assert.Equal(1, agingReport.Aging[0].Count);
            Assert.Equal(1, performanceReport.TotalClaims);
            Assert.Equal(1, performanceReport.CompletedClaims);
            Assert.Equal(0, performanceReport.ClaimsPending);
            Assert.Equal(0m, performanceReport.AverageTurnaroundDays);
            Assert.Equal(0, performanceReport.ClaimsOlderThan30Days);
            Assert.Equal(100m, performanceReport.SlaAttainmentPercentage);
            Assert.Equal(1, slaReport.EligibleClaims);
            Assert.Equal(1, slaReport.AchievedClaims);
            Assert.Equal(0, slaReport.BreachedClaims);
            Assert.Equal(100m, slaReport.AttainmentPercentage);

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
            Assert.Equal(
                ["ClaimSubmitted", "ClaimAssigned", "ClaimApproved", "ClaimSettled"],
                storedClaim.Notifications.Select(notification => notification.Category));

            var notificationService = new ClaimantNotificationService(
                new ClaimantNotificationRepository(dbContext),
                new ClaimantNotificationQueryValidator(),
                clock);
            var claimantId = storedClaim.Claimants.Single().Id;
            var notifications = await notificationService.GetNotificationsAsync(
                new ClaimantNotificationQuery(claimantId, null, false, 1, 100, false),
                CancellationToken.None);
            Assert.Equal(4, notifications.TotalCount);
            var notificationsByVerifiedEmail = await notificationService.GetNotificationsAsync(
                new ClaimantNotificationQuery(null, "claimant@example.com", true, 1, 1, false),
                CancellationToken.None);
            Assert.Equal(4, notificationsByVerifiedEmail.TotalCount);
            Assert.Single(notificationsByVerifiedEmail.Items);
            var notificationsForAnotherClaimant = await notificationService.GetNotificationsAsync(
                new ClaimantNotificationQuery(Guid.NewGuid(), null, false, 1, 100, false),
                CancellationToken.None);
            Assert.Empty(notificationsForAnotherClaimant.Items);
            var readNotification = await notificationService.MarkReadAsync(
                notifications.Items[0].Id,
                claimantId,
                null,
                false,
                CancellationToken.None);
            Assert.NotNull(readNotification?.ReadAtUtc);
            var unreadNotifications = await notificationService.GetNotificationsAsync(
                new ClaimantNotificationQuery(claimantId, null, false, 1, 100, true),
                CancellationToken.None);
            Assert.Equal(3, unreadNotifications.TotalCount);

            var ageBoundaryClaims = new[] { 0, 7, 8, 14, 15, 30, 31, 60, 61 }
                .Select(ageDays =>
                {
                    var reportedAtUtc = Now.AddDays(-ageDays);
                    return Claim.Create(
                        ClaimNumber.Create($"CLM-MY-{Guid.NewGuid():N}"),
                        policy,
                        ClaimType.Motor,
                        reportedAtUtc,
                        reportedAtUtc,
                        $"Aging boundary test at {ageDays} days.",
                        Money.Create(100m, "MYR"),
                        ClaimantDetails.Create(
                            ClaimantRole.PolicyHolder,
                            PersonName.Create("Aging Test Holder"),
                            ContactDetails.Create($"{Guid.NewGuid():N}@example.com", "+60123456789")));
                })
                .ToArray();
            dbContext.Claims.AddRange(ageBoundaryClaims);
            await dbContext.SaveChangesAsync();
            var boundaryAgingReport = await reportingService.GetAgingReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            Assert.Equal(9, boundaryAgingReport.TotalOpenClaims);
            Assert.Equal([2, 2, 2, 2, 1], boundaryAgingReport.Aging.Select(bucket => bucket.Count));

            var outboxEventTypes = await dbContext.OutboxMessages
                .Select(item => item.EventType)
                .ToArrayAsync();
            Assert.Single(outboxEventTypes, eventType => eventType == "ClaimSubmitted");
            Assert.Contains("ClaimAssigned", outboxEventTypes);
            Assert.Contains("ClaimApproved", outboxEventTypes);
            Assert.Contains("ClaimSettled", outboxEventTypes);

            var lateClaimReportedAtUtc = Now.AddDays(-15);
            var lateClaim = Claim.Create(
                ClaimNumber.Create($"CLM-MY-{Guid.NewGuid():N}"),
                policy,
                ClaimType.Motor,
                lateClaimReportedAtUtc,
                lateClaimReportedAtUtc,
                "Late decision performance test.",
                Money.Create(1_000m, "MYR"),
                ClaimantDetails.Create(
                    ClaimantRole.PolicyHolder,
                    PersonName.Create("Late Claim Holder"),
                    ContactDetails.Create("late-claim@example.com", "+60123456789")));
            dbContext.Claims.Add(lateClaim);
            await dbContext.SaveChangesAsync();
            await lifecycleService.ChangeWorkflowStatusAsync(
                new ChangeClaimWorkflowStatusCommand(
                    lateClaim.Number.Value,
                    handler.Id,
                    ClaimStatus.UnderReview,
                    "Late claim review started."),
                CancellationToken.None);
            await lifecycleService.RecordAssessmentAsync(
                new RecordClaimAssessmentCommand(
                    lateClaim.Number.Value,
                    assessor.Id,
                    500m,
                    400m,
                    "MYR",
                    "Late claim assessment."),
                CancellationToken.None);
            await lifecycleService.ApproveAsync(
                new ApproveClaimCommand(
                    lateClaim.Number.Value,
                    approver.Id,
                    400m,
                    "MYR",
                    "Late claim approved."),
                CancellationToken.None);

            var finalPerformance = await reportingService.GetPerformanceReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            var finalSla = await reportingService.GetSlaReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "MY"),
                CancellationToken.None);
            Assert.Equal(11, finalPerformance.TotalClaims);
            Assert.Equal(2, finalPerformance.CompletedClaims);
            Assert.Equal(9, finalPerformance.ClaimsPending);
            Assert.Equal(7.5m, finalPerformance.AverageTurnaroundDays);
            Assert.Equal(3, finalPerformance.ClaimsOlderThan30Days);
            Assert.Equal(50m, finalPerformance.SlaAttainmentPercentage);
            Assert.Equal(2, finalSla.EligibleClaims);
            Assert.Equal(1, finalSla.AchievedClaims);
            Assert.Equal(1, finalSla.BreachedClaims);
            Assert.Equal(50m, finalSla.AttainmentPercentage);
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
            .Include(claim => claim.Notifications)
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
