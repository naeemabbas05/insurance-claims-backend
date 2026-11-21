using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ClaimStaffOperationsTests
{
    private static readonly DateTimeOffset Now = Utc(2026, 11, 21);

    [Fact]
    public async Task AssignmentValidatorRequiresAnExplicitRole()
    {
        var result = await new AssignClaimCommandValidator().ValidateAsync(
            new AssignClaimCommand(
                Guid.NewGuid(),
                "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                Guid.NewGuid(),
                Role: null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(AssignClaimCommand.Role));
    }

    [Fact]
    public async Task IncomingQueueFiltersByMarketAndReturnsValidatedPages()
    {
        Claim[] claims =
        [
            CreateClaim("MY", "APAC-MY-QUEUE-001"),
            CreateClaim("MY", "APAC-MY-QUEUE-002"),
            CreateClaim("SG", "APAC-SG-QUEUE-001")
        ];
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var repository = new FakeClaimStaffOperationsRepository(claims, handler);
        var service = CreateService(repository);

        var page = await service.GetIncomingClaimsAsync(
            new ClaimMarketPageQuery(handler.Id, "MY", Page: 2, PageSize: 1),
            CancellationToken.None);

        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Single(page.Items);
        Assert.All(page.Items, item => Assert.Equal("MY", item.MarketCode));

        await Assert.ThrowsAsync<ValidationException>(() =>
            service.GetIncomingClaimsAsync(
                new ClaimMarketPageQuery(handler.Id, "MY", Page: 1, PageSize: 101),
                CancellationToken.None));
        await Assert.ThrowsAsync<StaffClaimAccessDeniedException>(() =>
            service.GetIncomingClaimsAsync(
                new ClaimMarketPageQuery(handler.Id, "SG"),
                CancellationToken.None));
    }

    [Fact]
    public async Task AssignmentAndReassignmentMaintainOfficerWorkloadAndHistory()
    {
        var claim = CreateClaim("MY", "APAC-MY-ASSIGN-001");
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var firstOfficer = CreateStaff(StaffRole.Adjuster, "MY");
        var secondOfficer = CreateStaff(StaffRole.Adjuster, "MY");
        var repository = new FakeClaimStaffOperationsRepository(
            [claim],
            handler,
            firstOfficer,
            secondOfficer);
        var eventOutbox = new RecordingClaimIntegrationEventOutbox();
        var service = CreateService(repository, eventOutbox);

        var firstAssignment = await service.AssignAsync(
            new AssignClaimCommand(
                handler.Id,
                claim.Number.Value,
                firstOfficer.Id,
                AssignmentRole.Adjuster),
            CancellationToken.None);
        var reassignment = await service.ReassignAsync(
            new ReassignClaimCommand(
                handler.Id,
                claim.Number.Value,
                firstAssignment.AssignmentId,
                secondOfficer.Id),
            CancellationToken.None);
        var history = await service.GetAssignmentHistoryAsync(
            new ClaimAssignmentHistoryQuery(handler.Id, claim.Number.Value),
            CancellationToken.None);
        var workload = await service.GetOfficerWorkloadAsync(
            new OfficerWorkloadQuery(handler.Id, secondOfficer.Id, "MY"),
            CancellationToken.None);

        Assert.NotEqual(firstAssignment.AssignmentId, reassignment.AssignmentId);
        Assert.Equal(2, history.Count);
        Assert.False(history[0].IsActive);
        Assert.True(history[1].IsActive);
        Assert.Equal(1, workload.ActiveClaimCount);
        Assert.Equal(1, workload.ByRole.Single().ActiveClaimCount);
        Assert.Equal(2, repository.SaveCount);
        Assert.Equal(2, eventOutbox.Events.Count(item => item is ClaimAssignedV1));
        Assert.Equal(
            ["ClaimSubmitted", "ClaimAssigned", "ClaimAssigned"],
            claim.Notifications.Select(notification => notification.Category));
    }

    private static ClaimStaffOperationsService CreateService(
        FakeClaimStaffOperationsRepository repository,
        IClaimIntegrationEventOutbox? eventOutbox = null) =>
        new(
            repository,
            new ClaimMarketPageQueryValidator(),
            new OfficerClaimsPageQueryValidator(),
            new OfficerWorkloadQueryValidator(),
            new ClaimAssignmentHistoryQueryValidator(),
            new AssignClaimCommandValidator(),
            new ReassignClaimCommandValidator(),
            eventOutbox ?? new RecordingClaimIntegrationEventOutbox(),
            new FixedTimeProvider(Now));

    private static Claim CreateClaim(string marketCode, string policyNumber)
    {
        var currency = marketCode == "SG" ? "SGD" : "MYR";
        var policy = Policy.Create(
            PolicyNumber.Create(policyNumber),
            MarketCatalog.Default,
            MarketCode.Create(marketCode),
            PolicyType.Motor,
            PersonName.Create("Claims Test User"),
            DateRange.Create(Utc(2026, 1, 1), Utc(2027, 1, 1)),
            Money.Create(50000m, currency),
            Utc(2025, 12, 1));
        return Claim.Create(
            ClaimNumber.Create($"CLM-{marketCode}-{Guid.NewGuid():N}"),
            policy,
            ClaimType.Motor,
            Utc(2026, 11, 20),
            Utc(2026, 11, 21),
            "Claim staff operations test.",
            Money.Create(1000m, currency),
            ClaimantDetails.Create(
                ClaimantRole.PolicyHolder,
                PersonName.Create("Claims Test User"),
                ContactDetails.Create("claims-test@example.com", null)));
    }

    private static StaffUser CreateStaff(StaffRole role, string marketCode) =>
        StaffUser.Create(
            PersonName.Create($"{role} Staff"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            role,
            [MarketCode.Create(marketCode)],
            MarketCatalog.Default,
            Utc(2025, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeClaimStaffOperationsRepository(
        IReadOnlyList<Claim> claims,
        params StaffUser[] staffUsers) : IClaimStaffOperationsRepository
    {
        private readonly Dictionary<Guid, StaffUser> _staffUsers =
            staffUsers.ToDictionary(staff => staff.Id);

        public int SaveCount { get; private set; }

        public Task<StaffUser?> FindStaffUserAsync(
            Guid staffUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_staffUsers.GetValueOrDefault(staffUserId));

        public Task<Claim?> FindClaimForAssignmentAsync(
            ClaimNumber claimNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult(claims.FirstOrDefault(claim => claim.Number == claimNumber));

        public Task<StaffClaimPage> FindIncomingClaimsAsync(
            MarketCode marketCode,
            int skip,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreatePage(
                claims.Where(claim =>
                    claim.Policy.MarketCode == marketCode &&
                    claim.Status == ClaimStatus.Submitted),
                skip,
                take));

        public Task<StaffClaimPage> FindUnassignedClaimsAsync(
            MarketCode marketCode,
            int skip,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreatePage(
                claims.Where(claim =>
                    claim.Policy.MarketCode == marketCode &&
                    claim.Status is not ClaimStatus.Closed and not ClaimStatus.Settled &&
                    claim.Assignments.All(assignment => !assignment.IsActive)),
                skip,
                take));

        public Task<StaffClaimPage> FindOfficerClaimsAsync(
            MarketCode marketCode,
            Guid officerId,
            int skip,
            int take,
            CancellationToken cancellationToken) =>
            Task.FromResult(CreatePage(
                claims.Where(claim =>
                    claim.Policy.MarketCode == marketCode &&
                    claim.Assignments.Any(assignment =>
                        assignment.IsActive && assignment.StaffUserId == officerId)),
                skip,
                take));

        public Task<StaffAssignmentWorkload> GetOfficerWorkloadAsync(
            MarketCode marketCode,
            Guid officerId,
            CancellationToken cancellationToken)
        {
            var assignments = claims
                .Where(claim =>
                    claim.Policy.MarketCode == marketCode &&
                    claim.Status is not ClaimStatus.Closed and not ClaimStatus.Settled)
                .SelectMany(claim => claim.Assignments)
                .Where(assignment => assignment.IsActive && assignment.StaffUserId == officerId)
                .ToArray();
            var byRole = assignments
                .GroupBy(assignment => assignment.Role)
                .Select(group => new AssignmentRoleWorkload(group.Key, group.Count()))
                .ToArray();
            return Task.FromResult(new StaffAssignmentWorkload(
                assignments.Select(assignment => assignment.ClaimId).Distinct().Count(),
                byRole));
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        private static StaffClaimPage CreatePage(IEnumerable<Claim> source, int skip, int take)
        {
            var filtered = source
                .OrderByDescending(claim => claim.ReportedAtUtc)
                .ThenBy(claim => claim.Id)
                .ToArray();
            return new StaffClaimPage(filtered.Skip(skip).Take(take).ToArray(), filtered.Length);
        }
    }
}
