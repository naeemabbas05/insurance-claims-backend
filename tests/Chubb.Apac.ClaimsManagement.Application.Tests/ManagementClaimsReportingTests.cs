using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

public sealed class ManagementClaimsReportingTests
{
    private static readonly DateTimeOffset Now = Utc(2026, 11, 21);

    [Fact]
    public async Task ReportUsesAuthorizedMarketsAndKeepsCurrencyTotalsSeparate()
    {
        var actor = CreateStaff(StaffRole.Approver, "MY", "SG");
        var repository = new FakeManagementClaimsReportRepository(actor, CreateReportData());
        var service = CreateService(repository);

        var result = await service.GetClaimsReportAsync(
            new ManagementClaimsReportQuery(
                actor.Id,
                FromDate: new DateOnly(2026, 11, 1),
                ToDate: new DateOnly(2026, 11, 21)),
            CancellationToken.None);

        Assert.Equal(12, result.TotalClaims);
        Assert.Equal(3, result.UnassignedClaims);
        Assert.Null(result.MarketCode);
        Assert.Equal(new DateOnly(2026, 11, 1), result.FromDate);
        Assert.Equal(new DateOnly(2026, 11, 21), result.ToDate);
        Assert.Equal(
            new[] { "MY", "SG" },
            repository.Markets!.Select(market => market.Value).OrderBy(code => code));
        Assert.Equal(Utc(2026, 11, 1), repository.FromDateUtc);
        Assert.Equal(Utc(2026, 11, 22), repository.ToDateExclusiveUtc);
        Assert.Equal(
            [new CurrencyAmountTotal("MYR", 500m), new CurrencyAmountTotal("SGD", 120m)],
            result.OutstandingEstimatedExposure);
        Assert.Equal(
            [new CurrencyAmountTotal("MYR", 300m)],
            result.ApprovedButUnpaidLiability);
        Assert.Equal(
            [new CurrencyAmountTotal("SGD", 450m)],
            result.SettledAmount);
        Assert.Equal(
            [new CurrencyAmountTotal("MYR", 900m), new CurrencyAmountTotal("SGD", 700m)],
            result.ApprovedAmount);
        Assert.Equal(
            [new CurrencyAmountTotal("MYR", 400m), new CurrencyAmountTotal("SGD", 250m)],
            result.AssessedAmount);
    }

    [Fact]
    public async Task ReportCanBeRestrictedToAnAuthorizedMarket()
    {
        var actor = CreateStaff(StaffRole.FinanceOfficer, "MY", "SG");
        var repository = new FakeManagementClaimsReportRepository(actor, CreateReportData());
        var service = CreateService(repository);

        var result = await service.GetClaimsReportAsync(
            new ManagementClaimsReportQuery(actor.Id, "sg"),
            CancellationToken.None);

        Assert.Equal("SG", result.MarketCode);
        Assert.Equal(["SG"], repository.Markets!.Select(market => market.Value));
        Assert.Null(repository.FromDateUtc);
        Assert.Null(repository.ToDateExclusiveUtc);
    }

    [Fact]
    public async Task ReportRejectsUnauthorizedRolesAndMarkets()
    {
        var handler = CreateStaff(StaffRole.ClaimsHandler, "MY");
        var unauthorizedRoleService = CreateService(
            new FakeManagementClaimsReportRepository(handler, CreateReportData()));
        await Assert.ThrowsAsync<ManagementReportAccessDeniedException>(() =>
            unauthorizedRoleService.GetClaimsReportAsync(
                new ManagementClaimsReportQuery(handler.Id),
                CancellationToken.None));

        var approver = CreateStaff(StaffRole.Approver, "MY");
        var unauthorizedMarketService = CreateService(
            new FakeManagementClaimsReportRepository(approver, CreateReportData()));
        await Assert.ThrowsAsync<ManagementReportAccessDeniedException>(() =>
            unauthorizedMarketService.GetClaimsReportAsync(
                new ManagementClaimsReportQuery(approver.Id, "SG"),
                CancellationToken.None));
    }

    [Fact]
    public async Task ValidatorRejectsInvalidMarketAndReversedDates()
    {
        var validator = new ManagementClaimsReportValidator();
        var marketResult = await validator.ValidateAsync(
            new ManagementClaimsReportQuery(Guid.NewGuid(), "Singapore"));
        var dateResult = await validator.ValidateAsync(
            new ManagementClaimsReportQuery(
                Guid.NewGuid(),
                FromDate: new DateOnly(2026, 11, 22),
                ToDate: new DateOnly(2026, 11, 21)));

        Assert.Contains(marketResult.Errors, error => error.PropertyName == nameof(ManagementClaimsReportQuery.MarketCode));
        Assert.Contains(dateResult.Errors, error => error.PropertyName == nameof(ManagementClaimsReportQuery.ToDate));
    }

    private static ManagementClaimsReportingService CreateService(
        IManagementClaimsReportRepository repository) =>
        new(repository, new ManagementClaimsReportValidator());

    private static ManagementClaimsReportData CreateReportData() =>
        new(
            TotalClaims: 12,
            UnassignedClaims: 3,
            ClaimsByStatus: [new ClaimCountByStatus(ClaimStatus.Submitted, 4)],
            ClaimsByMarket: [new ClaimCountByMarket("MY", 8), new ClaimCountByMarket("SG", 4)],
            ClaimsByType: [new ClaimCountByType(ClaimType.Motor, 7)],
            WorkloadByOfficer: [new OfficerClaimWorkload(Guid.NewGuid(), "Officer", 2)],
            OutstandingEstimatedExposure:
            [
                new CurrencyAmountTotal("MYR", 500m),
                new CurrencyAmountTotal("SGD", 120m)
            ],
            AssessedAmount:
            [
                new CurrencyAmountTotal("MYR", 400m),
                new CurrencyAmountTotal("SGD", 250m)
            ],
            ApprovedAmount:
            [
                new CurrencyAmountTotal("MYR", 900m),
                new CurrencyAmountTotal("SGD", 700m)
            ],
            ApprovedButUnpaidLiability: [new CurrencyAmountTotal("MYR", 300m)],
            SettledAmount: [new CurrencyAmountTotal("SGD", 450m)]);

    private static StaffUser CreateStaff(StaffRole role, params string[] marketCodes) =>
        StaffUser.Create(
            PersonName.Create($"{role} Staff"),
            EmailAddress.Create($"{Guid.NewGuid():N}@example.com"),
            role,
            marketCodes.Select(MarketCode.Create),
            MarketCatalog.Default,
            Utc(2025, 1, 1));

    private static DateTimeOffset Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, TimeSpan.Zero);

    private sealed class FakeManagementClaimsReportRepository(
        StaffUser staffUser,
        ManagementClaimsReportData report) : IManagementClaimsReportRepository
    {
        public IReadOnlyCollection<MarketCode>? Markets { get; private set; }

        public DateTimeOffset? FromDateUtc { get; private set; }

        public DateTimeOffset? ToDateExclusiveUtc { get; private set; }

        public Task<StaffUser?> FindStaffUserAsync(
            Guid staffUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(staffUserId == staffUser.Id ? staffUser : null);

        public Task<ManagementClaimsReportData> GetReportAsync(
            IReadOnlyCollection<MarketCode> marketCodes,
            DateTimeOffset? fromDateUtc,
            DateTimeOffset? toDateExclusiveUtc,
            CancellationToken cancellationToken)
        {
            Markets = marketCodes;
            FromDateUtc = fromDateUtc;
            ToDateExclusiveUtc = toDateExclusiveUtc;
            return Task.FromResult(report);
        }
    }
}
