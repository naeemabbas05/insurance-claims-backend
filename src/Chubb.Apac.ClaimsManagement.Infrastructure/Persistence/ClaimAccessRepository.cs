using Chubb.Apac.ClaimsManagement.Application.Security;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimAccessRepository(ClaimsDbContext dbContext) : IClaimAccessRepository
{
    public async Task<ClaimAccessData?> FindClaimAccessDataAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken)
    {
        var claim = await dbContext.Claims
            .AsNoTracking()
            .Include(item => item.Policy)
            .Include(item => item.Claimants)
            .AsSplitQuery()
            .SingleOrDefaultAsync(item => item.Number == claimNumber, cancellationToken);

        return claim is null
            ? null
            : new ClaimAccessData(
                claim.Policy.MarketCode.Value,
                claim.Claimants
                    .Select(claimant => new ClaimantAccessIdentity(
                        claimant.Id,
                        claimant.Details.Contact.Email?.Value))
                    .ToArray());
    }

    public Task<StaffUser?> FindStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken) =>
        dbContext.StaffUsers
            .AsNoTracking()
            .Include(staffUser => staffUser.Markets)
            .SingleOrDefaultAsync(staffUser => staffUser.Id == staffUserId, cancellationToken);
}
