using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimStaffOperationsRepository(ClaimsDbContext dbContext)
    : IClaimStaffOperationsRepository
{
    public Task<StaffUser?> FindStaffUserAsync(
        Guid staffUserId,
        CancellationToken cancellationToken) =>
        dbContext.StaffUsers
            .Include(staffUser => staffUser.Markets)
            .SingleOrDefaultAsync(staffUser => staffUser.Id == staffUserId, cancellationToken);

    public Task<Claim?> FindClaimForAssignmentAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken) =>
        dbContext.Claims
            .Include(claim => claim.Policy)
                .ThenInclude(policy => policy.Market)
            .Include(claim => claim.Assignments)
                .ThenInclude(assignment => assignment.StaffUser)
            .AsSplitQuery()
            .SingleOrDefaultAsync(claim => claim.Number == claimNumber, cancellationToken);

    public async Task<StaffClaimPage> FindIncomingClaimsAsync(
        MarketCode marketCode,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Claims
            .AsNoTracking()
            .Where(claim =>
                claim.Policy.MarketCode == marketCode &&
                claim.Status == ClaimStatus.Submitted);
        var totalCount = await query.CountAsync(cancellationToken);
        var claims = await query
            .Include(claim => claim.Policy)
            .Include(claim => claim.Assignments.Where(assignment => assignment.UnassignedAtUtc == null))
                .ThenInclude(assignment => assignment.StaffUser)
            .OrderByDescending(claim => claim.ReportedAtUtc)
            .ThenBy(claim => claim.Id)
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToArrayAsync(cancellationToken);
        return new StaffClaimPage(claims, totalCount);
    }

    public async Task<StaffClaimPage> FindUnassignedClaimsAsync(
        MarketCode marketCode,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Claims
            .AsNoTracking()
            .Where(claim =>
                claim.Policy.MarketCode == marketCode &&
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled &&
                !claim.Assignments.Any(assignment => assignment.UnassignedAtUtc == null));
        var totalCount = await query.CountAsync(cancellationToken);
        var claims = await query
            .Include(claim => claim.Policy)
            .OrderByDescending(claim => claim.ReportedAtUtc)
            .ThenBy(claim => claim.Id)
            .Skip(skip)
            .Take(take)
            .ToArrayAsync(cancellationToken);
        return new StaffClaimPage(claims, totalCount);
    }

    public async Task<StaffClaimPage> FindOfficerClaimsAsync(
        MarketCode marketCode,
        Guid officerId,
        int skip,
        int take,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Claims
            .AsNoTracking()
            .Where(claim =>
                claim.Policy.MarketCode == marketCode &&
                claim.Assignments.Any(assignment =>
                    assignment.StaffUserId == officerId &&
                    assignment.UnassignedAtUtc == null));
        var totalCount = await query.CountAsync(cancellationToken);
        var claims = await query
            .Include(claim => claim.Policy)
            .Include(claim => claim.Assignments.Where(assignment =>
                assignment.StaffUserId == officerId &&
                assignment.UnassignedAtUtc == null))
                .ThenInclude(assignment => assignment.StaffUser)
            .OrderByDescending(claim => claim.ReportedAtUtc)
            .ThenBy(claim => claim.Id)
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToArrayAsync(cancellationToken);
        return new StaffClaimPage(claims, totalCount);
    }

    public async Task<StaffAssignmentWorkload> GetOfficerWorkloadAsync(
        MarketCode marketCode,
        Guid officerId,
        CancellationToken cancellationToken)
    {
        var activeAssignments = dbContext.Claims
            .AsNoTracking()
            .Where(claim =>
                claim.Policy.MarketCode == marketCode &&
                claim.Status != ClaimStatus.Closed &&
                claim.Status != ClaimStatus.Settled)
            .SelectMany(claim => claim.Assignments
                .Where(assignment =>
                    assignment.StaffUserId == officerId &&
                    assignment.UnassignedAtUtc == null)
                .Select(assignment => new
                {
                    assignment.ClaimId,
                    assignment.Role
                }));

        var activeClaimCount = await activeAssignments
            .Select(assignment => assignment.ClaimId)
            .Distinct()
            .CountAsync(cancellationToken);
        var roleCounts = await activeAssignments
            .GroupBy(assignment => assignment.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .OrderBy(item => item.Role)
            .ToArrayAsync(cancellationToken);
        return new StaffAssignmentWorkload(
            activeClaimCount,
            roleCounts.Select(item => new AssignmentRoleWorkload(item.Role, item.Count)).ToArray());
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
