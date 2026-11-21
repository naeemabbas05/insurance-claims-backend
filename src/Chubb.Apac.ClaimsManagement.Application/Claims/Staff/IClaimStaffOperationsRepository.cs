using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public interface IClaimStaffOperationsRepository
{
    Task<StaffUser?> FindStaffUserAsync(Guid staffUserId, CancellationToken cancellationToken);

    Task<Claim?> FindClaimForAssignmentAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken);

    Task<StaffClaimPage> FindIncomingClaimsAsync(
        MarketCode marketCode,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<StaffClaimPage> FindUnassignedClaimsAsync(
        MarketCode marketCode,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<StaffClaimPage> FindOfficerClaimsAsync(
        MarketCode marketCode,
        Guid officerId,
        int skip,
        int take,
        CancellationToken cancellationToken);

    Task<StaffAssignmentWorkload> GetOfficerWorkloadAsync(
        MarketCode marketCode,
        Guid officerId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
