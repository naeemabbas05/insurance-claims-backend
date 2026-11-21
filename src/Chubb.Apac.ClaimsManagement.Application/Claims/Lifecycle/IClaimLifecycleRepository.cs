using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public interface IClaimLifecycleRepository
{
    Task<Claim?> FindClaimAsync(ClaimNumber claimNumber, CancellationToken cancellationToken);

    Task<StaffUser?> FindStaffUserAsync(Guid staffUserId, CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
