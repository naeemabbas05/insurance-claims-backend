using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Security;

public interface IClaimAccessRepository
{
    Task<ClaimAccessData?> FindClaimAccessDataAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken);

    Task<StaffUser?> FindStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken);
}
