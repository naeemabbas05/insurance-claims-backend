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
            report.Aging);
    }

    public async Task<ManagementClaimsPerformanceReportResult> GetPerformanceReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        ValidateSlaTarget();
        var scope = await GetScopeAsync(query, cancellationToken);
        var asOfUtc = timeProvider.GetUtcNow();
        var report = await repository.GetPerformanceReportAsync(
            scope.Markets,
            scope.FromDateUtc,
            scope.ToDateExclusiveUtc,
            asOfUtc,
            reportingOptions.SlaTargetDays,
            cancellationToken);

        return new ManagementClaimsPerformanceReportResult(
            scope.MarketCode,
            query.FromDate,
            query.ToDate,
            asOfUtc,
            report.TotalClaims,
            report.CompletedClaims,
            report.ClaimsPending,
            report.AverageTurnaroundDays,
            report.ClaimsOlderThan30Days,
            CalculateAttainmentPercentage(report.SlaAchievedClaims, report.EligibleSlaClaims));
    }

    public async Task<ManagementClaimsSlaReportResult> GetSlaReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken)
    {
        ValidateSlaTarget();
        var scope = await GetScopeAsync(query, cancellationToken);
        var asOfUtc = timeProvider.GetUtcNow();
        var report = await repository.GetPerformanceReportAsync(
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
            report.EligibleSlaClaims,
            report.SlaAchievedClaims,
            report.SlaBreachedClaims,
            CalculateAttainmentPercentage(report.SlaAchievedClaims, report.EligibleSlaClaims));
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

    private void ValidateSlaTarget()
    {
        if (reportingOptions.SlaTargetDays <= 0)
        {
            throw new InvalidOperationException(
                "ManagementReporting:SlaTargetDays must be greater than zero.");
        }
    }

    private static decimal CalculateAttainmentPercentage(int achieved, int eligible) =>
        eligible == 0 ? 0m : decimal.Round((decimal)achieved / eligible * 100m, 2);

    private sealed record ReportingScope(
        IReadOnlyCollection<MarketCode> Markets,
        DateTimeOffset? FromDateUtc,
        DateTimeOffset? ToDateExclusiveUtc,
        string? MarketCode);
}
