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
