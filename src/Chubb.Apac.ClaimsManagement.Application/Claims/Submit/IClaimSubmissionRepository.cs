using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public interface IClaimSubmissionRepository
{
    Task<bool> IsMarketConfiguredAsync(MarketCode marketCode, CancellationToken cancellationToken);

    Task<Policy?> FindPolicyAsync(
        MarketCode marketCode,
        PolicyNumber policyNumber,
        CancellationToken cancellationToken);

    Task<StoredClaimSubmission?> FindByIdempotencyKeyHashAsync(
        string idempotencyKeyHash,
        CancellationToken cancellationToken);

    Task<StoredClaimSubmission> SaveIdempotentlyAsync(
        Claim claim,
        string idempotencyKeyHash,
        string requestFingerprint,
        CancellationToken cancellationToken);

    Task<ClaimTrackingResult?> FindByClaimReferenceAsync(
        ClaimNumber claimNumber,
        CancellationToken cancellationToken);
}
