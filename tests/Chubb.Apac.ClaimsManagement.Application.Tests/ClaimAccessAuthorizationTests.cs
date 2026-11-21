using Chubb.Apac.ClaimsManagement.Application.Security;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ClaimAccessAuthorizationTests
{
    private static readonly Guid ClaimantId = Guid.NewGuid();

    [Fact]
    public async Task VerifiedOwnerEmailCanAccessClaimAndResolveClaimantId()
    {
        var service = CreateService();

        var access = await service.AuthorizeAsync(
            new ClaimAccessQuery(
                "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                ClaimAccessRole.Claimant,
                Email: "OWNER@example.com",
                EmailVerified: true),
            CancellationToken.None);

        Assert.Equal(ClaimantId, access.ClaimantId);
    }

    [Fact]
    public async Task UnverifiedEmailAndOtherClaimantCannotAccessClaim()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeAsync(
                new ClaimAccessQuery(
                    "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                    ClaimAccessRole.Claimant,
                    Email: "owner@example.com",
                    EmailVerified: false),
                CancellationToken.None));
        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeAsync(
                new ClaimAccessQuery(
                    "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                    ClaimAccessRole.Claimant,
                    ClaimantId: Guid.NewGuid()),
                CancellationToken.None));
    }

    [Fact]
    public async Task StaffAccessRequiresRoleAndClaimMarketMembership()
    {
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var foreignMarketHandler = CreateStaff(StaffRole.ClaimsHandler, "SG");
        var approver = CreateStaff(StaffRole.Approver, "MY");
        var administrator = CreateStaff(StaffRole.Administrator, "MY");
        var service = CreateService(handler, foreignMarketHandler, approver, administrator);

        await service.AuthorizeAsync(
            new ClaimAccessQuery(
                "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                ClaimAccessRole.ClaimsOfficer,
                StaffUserId: handler.Id),
            CancellationToken.None);

        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeAsync(
                new ClaimAccessQuery(
                    "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                    ClaimAccessRole.ClaimsOfficer,
                    StaffUserId: administrator.Id),
                CancellationToken.None));
        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeAsync(
                new ClaimAccessQuery(
                    "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                    ClaimAccessRole.ClaimsOfficer,
                    StaffUserId: foreignMarketHandler.Id),
                CancellationToken.None));
        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeAsync(
                new ClaimAccessQuery(
                    "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                    ClaimAccessRole.Manager,
                    StaffUserId: handler.Id),
                CancellationToken.None));
        await service.AuthorizeAsync(
            new ClaimAccessQuery(
                "CLM-MY-0123456789ABCDEF0123456789ABCDEF",
                ClaimAccessRole.Manager,
                StaffUserId: administrator.Id),
            CancellationToken.None);
    }

    [Fact]
    public async Task StaffMarketAccessRequiresMatchingIdentityRoleAndMarket()
    {
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var administrator = CreateStaff(StaffRole.Administrator, "MY");
        var service = CreateService(handler, administrator);

        await service.AuthorizeStaffAsync(
            handler.Id,
            ClaimAccessRole.ClaimsOfficer,
            "MY",
            CancellationToken.None);
        await service.AuthorizeStaffAsync(
            administrator.Id,
            ClaimAccessRole.Manager,
            "MY",
            CancellationToken.None);

        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeStaffAsync(
                administrator.Id,
                ClaimAccessRole.ClaimsOfficer,
                "MY",
                CancellationToken.None));
        await Assert.ThrowsAsync<ClaimAccessDeniedException>(() =>
            service.AuthorizeStaffAsync(
                handler.Id,
                ClaimAccessRole.ClaimsOfficer,
                "SG",
                CancellationToken.None));
    }

    private static ClaimAccessAuthorizationService CreateService(params StaffUser[] staffUsers) =>
        new(new FakeClaimAccessRepository(staffUsers));

    private static StaffUser CreateStaff(StaffRole role, params string[] markets) =>
        StaffUser.Create(
            PersonName.Create($"{role} Staff"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            role,
            markets.Select(MarketCode.Create),
            MarketCatalog.Default,
            Utc(2025, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FakeClaimAccessRepository(params StaffUser[] staffUsers)
        : IClaimAccessRepository
    {
        private readonly Dictionary<Guid, StaffUser> _staffUsers =
            staffUsers.ToDictionary(staff => staff.Id);

        public Task<ClaimAccessData?> FindClaimAccessDataAsync(
            ClaimNumber claimNumber,
            CancellationToken cancellationToken) =>
            Task.FromResult<ClaimAccessData?>(
                claimNumber.Value == "CLM-MY-0123456789ABCDEF0123456789ABCDEF"
                    ? new ClaimAccessData(
                        "MY",
                        [new ClaimantAccessIdentity(ClaimantId, "owner@example.com")])
                    : null);

        public Task<StaffUser?> FindStaffUserAsync(
            Guid staffUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(_staffUsers.GetValueOrDefault(staffUserId));
    }
}
