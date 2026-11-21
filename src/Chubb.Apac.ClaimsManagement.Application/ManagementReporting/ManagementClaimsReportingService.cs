using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public sealed class ManagementClaimsReportingService(
    IManagementClaimsReportRepository repository,
    IValidator<ManagementClaimsReportQuery> queryValidator,
    ManagementReportingOptions reportingOptions,
    TimeProvider timeProvider) : IManagementClaimsReportingService
{
    public async Task<ManagementClaimsReportResult> GetClaimsReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(query, cancellationToken);
        var report = await repository.GetReportAsync(
            scope.Markets,
            scope.FromDateUtc,
            scope.ToDateExclusiveUtc,
            cancellationToken);

        return new ManagementClaimsReportResult(
            query.MarketCode?.ToUpperInvariant(),
            query.FromDate,
            query.ToDate,
            report.TotalClaims,
            report.UnassignedClaims,
            report.ClaimsByStatus,
            report.ClaimsByMarket,
            report.ClaimsByType,
            report.WorkloadByOfficer,
            report.OutstandingEstimatedExposure,
            report.AssessedAmount,
            report.ApprovedAmount,
            report.ApprovedButUnpaidLiability,
            report.SettledAmount);
    }

    public async Task<ManagementClaimsAgingReportResult> GetAgingReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(query, cancellationToken);
        var asOfUtc = timeProvider.GetUtcNow();
        var report = await repository.GetAgingReportAsync(
            scope.Markets,
            scope.FromDateUtc,
            scope.ToDateExclusiveUtc,
            asOfUtc,
            cancellationToken);

        return new ManagementClaimsAgingReportResult(
            scope.MarketCode,
            query.FromDate,
            query.ToDate,
            asOfUtc,
            report.TotalOpenClaims,
            report.Markets);
    }

    public async Task<ManagementClaimsPerformanceReportResult> GetPerformanceReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        var scope = await GetScopeAsync(query, cancellationToken);
        var report = await repository.GetPerformanceReportAsync(
            scope.Markets,
            scope.FromDateUtc,
            scope.ToDateExclusiveUtc,
            cancellationToken);

        return new ManagementClaimsPerformanceReportResult(
            scope.MarketCode,
            query.FromDate,
            query.ToDate,
            report.TotalDecisions,
            report.Officers);
    }

    public async Task<ManagementClaimsSlaReportResult> GetSlaReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        if (reportingOptions.SlaTargetDays <= 0)
        {
            throw new InvalidOperationException(
                "ManagementReporting:SlaTargetDays must be greater than zero.");
        }

        var scope = await GetScopeAsync(query, cancellationToken);
        var asOfUtc = timeProvider.GetUtcNow();
        var report = await repository.GetSlaReportAsync(
            scope.Markets,
            scope.FromDateUtc,
            scope.ToDateExclusiveUtc,
            asOfUtc,
            reportingOptions.SlaTargetDays,
            cancellationToken);

        return new ManagementClaimsSlaReportResult(
            scope.MarketCode,
            query.FromDate,
            query.ToDate,
            reportingOptions.SlaTargetDays,
            asOfUtc,
            report.TotalOpenClaims,
            report.WithinTarget,
            report.Breached,
            CalculateBreachRate(report.Breached, report.TotalOpenClaims),
            report.Markets);
    }

    private async Task<ReportingScope> GetScopeAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var actor = await repository.FindStaffUserAsync(query.ActorStaffUserId, cancellationToken);
        if (actor is not { IsActive: true } ||
            (!actor.CanPerform(StaffRole.Approver) && !actor.CanPerform(StaffRole.FinanceOfficer)))
        {
            throw new ManagementReportAccessDeniedException();
        }

        IReadOnlyCollection<MarketCode> markets;
        if (query.MarketCode is not null)
        {
            var marketCode = MarketCode.Create(query.MarketCode);
            if (!actor.CanWorkIn(marketCode))
            {
                throw new ManagementReportAccessDeniedException();
            }

            markets = [marketCode];
        }
        else
        {
            markets = actor.Markets
                .Select(market => market.Code)
                .Distinct()
                .ToArray();
        }

        DateTimeOffset? fromDateUtc = query.FromDate is { } fromDate
            ? new DateTimeOffset(fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        DateTimeOffset? toDateExclusiveUtc = query.ToDate is { } toDate && toDate < DateOnly.MaxValue
            ? new DateTimeOffset(toDate.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))
            : null;
        return new ReportingScope(
            markets,
            fromDateUtc,
            toDateExclusiveUtc,
            query.MarketCode?.ToUpperInvariant());
    }

    private static decimal CalculateBreachRate(int breached, int total) =>
        total == 0 ? 0m : decimal.Round((decimal)breached / total * 100m, 2);

    private sealed record ReportingScope(
        IReadOnlyCollection<MarketCode> Markets,
        DateTimeOffset? FromDateUtc,
        DateTimeOffset? ToDateExclusiveUtc,
        string? MarketCode);
}
