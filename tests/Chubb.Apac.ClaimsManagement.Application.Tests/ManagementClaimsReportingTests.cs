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

    [Fact]
    public async Task AdditionalReportsUseAuthorizedMarketsAndConfiguredSlaTarget()
    {
        var actor = CreateStaff(StaffRole.Approver, "MY", "SG");
        var repository = new FakeManagementClaimsReportRepository(actor, CreateReportData());
        var service = CreateService(repository);
        var query = new ManagementClaimsReportQuery(
            actor.Id,
            "MY",
            new DateOnly(2026, 11, 1),
            new DateOnly(2026, 11, 21));

        var aging = await service.GetAgingReportAsync(query, CancellationToken.None);
        var performance = await service.GetPerformanceReportAsync(query, CancellationToken.None);
        var sla = await service.GetSlaReportAsync(query, CancellationToken.None);

        Assert.Equal(3, aging.TotalOpenClaims);
        Assert.Equal(Utc(2026, 11, 21), aging.AsOfUtc);
        Assert.Equal(2, performance.TotalDecisions);
        Assert.Equal(4.5m, Assert.Single(performance.Officers).AverageProcessingDays);
        Assert.Equal(10, sla.TargetDays);
        Assert.Equal(1, sla.Breached);
        Assert.Equal(33.33m, sla.BreachRatePercentage);
        Assert.All(
            new[] { repository.AgingMarkets, repository.PerformanceMarkets, repository.SlaMarkets },
            markets => Assert.Equal(["MY"], markets!.Select(market => market.Value)));
    }

    [Fact]
    public async Task SlaReportUsesZeroBreachRateWhenNoClaimsAreInScope()
    {
        var actor = CreateStaff(StaffRole.Approver, "MY");
        var repository = new FakeManagementClaimsReportRepository(actor, CreateReportData())
        {
            SlaReport = new ClaimsSlaReportData(0, 0, 0, [])
        };
        var service = CreateService(repository);

        var report = await service.GetSlaReportAsync(
            new ManagementClaimsReportQuery(actor.Id, "MY"),
            CancellationToken.None);

        Assert.Equal(0, report.TotalOpenClaims);
        Assert.Equal(0m, report.BreachRatePercentage);
        Assert.Empty(report.Markets);
    }

    private static ManagementClaimsReportingService CreateService(
        IManagementClaimsReportRepository repository) =>
        new(
            repository,
            new ManagementClaimsReportValidator(),
            new ManagementReportingOptions(10),
            new FixedTimeProvider(Now));

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

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeManagementClaimsReportRepository(
        StaffUser staffUser,
        ManagementClaimsReportData report) : IManagementClaimsReportRepository
    {
        public IReadOnlyCollection<MarketCode>? Markets { get; private set; }

        public DateTimeOffset? FromDateUtc { get; private set; }

        public DateTimeOffset? ToDateExclusiveUtc { get; private set; }

        public IReadOnlyCollection<MarketCode>? AgingMarkets { get; private set; }

        public IReadOnlyCollection<MarketCode>? PerformanceMarkets { get; private set; }

        public IReadOnlyCollection<MarketCode>? SlaMarkets { get; private set; }

        public ClaimsSlaReportData SlaReport { get; init; } =
            new(3, 2, 1, [new ClaimSlaMarketResult("MY", 3, 2, 1, 33.33m)]);

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

        public Task<ClaimsAgingReportData> GetAgingReportAsync(
            IReadOnlyCollection<MarketCode> marketCodes,
            DateTimeOffset? fromDateUtc,
            DateTimeOffset? toDateExclusiveUtc,
            DateTimeOffset asOfUtc,
            CancellationToken cancellationToken)
        {
            AgingMarkets = marketCodes;
            return Task.FromResult(new ClaimsAgingReportData(
                3,
                [
                    new ClaimAgingMarketResult(
                        "MY",
                        3,
                        [
                            new ClaimAgingBucket("0-7 days", 1),
                            new ClaimAgingBucket("8-14 days", 1),
                            new ClaimAgingBucket("15-30 days", 1),
                            new ClaimAgingBucket("31+ days", 0)
                        ])
                ]));
        }

        public Task<ClaimsPerformanceReportData> GetPerformanceReportAsync(
            IReadOnlyCollection<MarketCode> marketCodes,
            DateTimeOffset? fromDateUtc,
            DateTimeOffset? toDateExclusiveUtc,
            CancellationToken cancellationToken)
        {
            PerformanceMarkets = marketCodes;
            return Task.FromResult(new ClaimsPerformanceReportData(
                2,
                [new OfficerDecisionPerformance(Guid.NewGuid(), "Officer", 2, 1, 1, 4.5m)]));
        }

        public Task<ClaimsSlaReportData> GetSlaReportAsync(
            IReadOnlyCollection<MarketCode> marketCodes,
            DateTimeOffset? fromDateUtc,
            DateTimeOffset? toDateExclusiveUtc,
            DateTimeOffset asOfUtc,
            int targetDays,
            CancellationToken cancellationToken)
        {
            SlaMarkets = marketCodes;
            return Task.FromResult(SlaReport);
        }
    }
}
