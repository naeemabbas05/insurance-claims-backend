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
}
