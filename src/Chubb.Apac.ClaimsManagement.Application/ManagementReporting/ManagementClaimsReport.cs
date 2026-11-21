using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public sealed record ManagementClaimsReportQuery(
    Guid ActorStaffUserId,
    string? MarketCode = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null);

public sealed record ClaimCountByStatus(ClaimStatus Status, int Count);

public sealed record ClaimCountByMarket(string MarketCode, int Count);

public sealed record ClaimCountByType(ClaimType Type, int Count);

public sealed record OfficerClaimWorkload(Guid OfficerId, string OfficerName, int ActiveClaimCount);

public sealed record CurrencyAmountTotal(string CurrencyCode, decimal Amount);

public sealed record ManagementClaimsReportResult(
    string? MarketCode,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalClaims,
    int UnassignedClaims,
    IReadOnlyList<ClaimCountByStatus> ClaimsByStatus,
    IReadOnlyList<ClaimCountByMarket> ClaimsByMarket,
    IReadOnlyList<ClaimCountByType> ClaimsByType,
    IReadOnlyList<OfficerClaimWorkload> WorkloadByOfficer,
    IReadOnlyList<CurrencyAmountTotal> OutstandingEstimatedExposure,
    IReadOnlyList<CurrencyAmountTotal> AssessedAmount,
    IReadOnlyList<CurrencyAmountTotal> ApprovedAmount,
    IReadOnlyList<CurrencyAmountTotal> ApprovedButUnpaidLiability,
    IReadOnlyList<CurrencyAmountTotal> SettledAmount);

public sealed record ManagementClaimsReportData(
    int TotalClaims,
    int UnassignedClaims,
    IReadOnlyList<ClaimCountByStatus> ClaimsByStatus,
    IReadOnlyList<ClaimCountByMarket> ClaimsByMarket,
    IReadOnlyList<ClaimCountByType> ClaimsByType,
    IReadOnlyList<OfficerClaimWorkload> WorkloadByOfficer,
    IReadOnlyList<CurrencyAmountTotal> OutstandingEstimatedExposure,
    IReadOnlyList<CurrencyAmountTotal> AssessedAmount,
    IReadOnlyList<CurrencyAmountTotal> ApprovedAmount,
    IReadOnlyList<CurrencyAmountTotal> ApprovedButUnpaidLiability,
    IReadOnlyList<CurrencyAmountTotal> SettledAmount);

public sealed record ClaimAgingBucket(string Label, int ClaimCount);

public sealed record ClaimAgingMarketResult(
    string MarketCode,
    int TotalOpenClaims,
    IReadOnlyList<ClaimAgingBucket> Buckets);

public sealed record ManagementClaimsAgingReportResult(
    string? MarketCode,
    DateOnly? FromDate,
    DateOnly? ToDate,
    DateTimeOffset AsOfUtc,
    int TotalOpenClaims,
    IReadOnlyList<ClaimAgingMarketResult> Markets);

public sealed record ClaimsAgingReportData(
    int TotalOpenClaims,
    IReadOnlyList<ClaimAgingMarketResult> Markets);

public sealed record OfficerDecisionPerformance(
    Guid OfficerId,
    string OfficerName,
    int DecisionCount,
    int ApprovedCount,
    int DeclinedCount,
    decimal AverageProcessingDays);

public sealed record ManagementClaimsPerformanceReportResult(
    string? MarketCode,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TotalDecisions,
    IReadOnlyList<OfficerDecisionPerformance> Officers);

public sealed record ClaimsPerformanceReportData(
    int TotalDecisions,
    IReadOnlyList<OfficerDecisionPerformance> Officers);

public sealed record ClaimSlaMarketResult(
    string MarketCode,
    int TotalOpenClaims,
    int WithinTarget,
    int Breached,
    decimal BreachRatePercentage);

public sealed record ManagementClaimsSlaReportResult(
    string? MarketCode,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int TargetDays,
    DateTimeOffset AsOfUtc,
    int TotalOpenClaims,
    int WithinTarget,
    int Breached,
    decimal BreachRatePercentage,
    IReadOnlyList<ClaimSlaMarketResult> Markets);

public sealed record ClaimsSlaReportData(
    int TotalOpenClaims,
    int WithinTarget,
    int Breached,
    IReadOnlyList<ClaimSlaMarketResult> Markets);

public sealed record ManagementReportingOptions(int SlaTargetDays = 14);
