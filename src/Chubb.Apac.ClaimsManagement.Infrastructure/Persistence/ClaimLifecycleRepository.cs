using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimLifecycleRepository(ClaimsDbContext dbContext) : IClaimLifecycleRepository
{
    public Task<Claim?> FindClaimAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken) =>
        dbContext.Claims
            .Include(claim => claim.Policy)
                .ThenInclude(policy => policy.Market)
            .Include(claim => claim.Claimants)
            .Include(claim => claim.Assessments)
            .Include(claim => claim.StatusHistory)
            .Include(claim => claim.InformationRequests)
            .Include(claim => claim.Settlements)
            .Include(claim => claim.AuditHistory)
            .AsSplitQuery()
            .SingleOrDefaultAsync(claim => claim.Number == claimNumber, cancellationToken);

    public Task<StaffUser?> FindStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken) =>
        dbContext.StaffUsers
            .Include(staffUser => staffUser.Markets)
            .SingleOrDefaultAsync(staffUser => staffUser.Id == staffUserId, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
