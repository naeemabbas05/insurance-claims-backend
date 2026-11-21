using System.Data;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimSubmissionRepository(ClaimsDbContext dbContext) : IClaimSubmissionRepository
{
    public Task<bool> IsMarketConfiguredAsync(
        MarketCode marketCode,
        CancellationToken cancellationToken) =>
        dbContext.Markets.AnyAsync(
            market => market.Code == marketCode,
            cancellationToken);

    public Task<Policy?> FindPolicyAsync(
        MarketCode marketCode,
        PolicyNumber policyNumber,
        CancellationToken cancellationToken) =>
        dbContext.Policies
            .Include(policy => policy.Market)
            .SingleOrDefaultAsync(
                policy => policy.MarketCode == marketCode && policy.Number == policyNumber,
                cancellationToken);

    public async Task<StoredClaimSubmission?> FindByIdempotencyKeyHashAsync(
        string idempotencyKeyHash,
        CancellationToken cancellationToken)
    {
        var record = await dbContext.ClaimSubmissionIdempotencyRecords
            .AsNoTracking()
            .Include(submission => submission.Claim)
            .SingleOrDefaultAsync(
                submission => submission.KeyHash == idempotencyKeyHash,
                cancellationToken);

        return record is null ? null : ToStoredSubmission(record, isReplay: true);
    }

    public async Task<StoredClaimSubmission> SaveIdempotentlyAsync(
        Claim claim,
        string idempotencyKeyHash,
        string requestFingerprint,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var existing = await dbContext.ClaimSubmissionIdempotencyRecords
            .AsNoTracking()
            .Include(submission => submission.Claim)
            .SingleOrDefaultAsync(
                submission => submission.KeyHash == idempotencyKeyHash,
                cancellationToken);
        if (existing is not null)
        {
            await transaction.CommitAsync(cancellationToken);
            return ToStoredSubmission(existing, isReplay: true);
        }

        dbContext.Claims.Add(claim);
        dbContext.ClaimSubmissionIdempotencyRecords.Add(
            new ClaimSubmissionIdempotencyRecord(
                idempotencyKeyHash,
                requestFingerprint,
                claim,
                claim.ReportedAtUtc));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return ToStoredSubmission(
            new ClaimSubmissionIdempotencyRecord(
                idempotencyKeyHash,
                requestFingerprint,
                claim,
                claim.ReportedAtUtc),
            isReplay: false);
    }

    public async Task<ClaimTrackingResult?> FindByClaimReferenceAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken)
    {
        var claim = await dbContext.Claims
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.Number == claimNumber,
                cancellationToken);

        return claim is null
            ? null
            : new ClaimTrackingResult(
                claim.Number.Value,
                claim.Status,
                claim.ReportedAtUtc);
    }

    private static StoredClaimSubmission ToStoredSubmission(
        ClaimSubmissionIdempotencyRecord record,
        bool isReplay) =>
        new(
            record.RequestFingerprint,
            record.Claim.Number.Value,
            record.Claim.Status,
            record.Claim.ReportedAtUtc,
            isReplay);
}
