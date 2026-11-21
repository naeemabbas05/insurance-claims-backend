using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public interface IManagementClaimsReportRepository
{
    Task<StaffUser?> FindStaffUserAsync(Guid staffUserId, CancellationToken cancellationToken);

    Task<ManagementClaimsReportData> GetReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        CancellationToken cancellationToken);

    Task<ClaimsAgingReportData> GetAgingReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        DateTimeOffset asOfUtc,
        CancellationToken cancellationToken);

    Task<ClaimsPerformanceReportData> GetPerformanceReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        CancellationToken cancellationToken);

    Task<ClaimsSlaReportData> GetSlaReportAsync(
        IReadOnlyCollection<MarketCode> marketCodes,
        DateTimeOffset? fromDateUtc,
        DateTimeOffset? toDateExclusiveUtc,
        DateTimeOffset asOfUtc,
        int targetDays,
        CancellationToken cancellationToken);
}
