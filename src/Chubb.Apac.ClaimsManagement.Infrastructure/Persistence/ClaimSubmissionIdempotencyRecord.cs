using Chubb.Apac.ClaimsManagement.Domain.Entities;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimSubmissionIdempotencyRecord
{
    private ClaimSubmissionIdempotencyRecord()
    {
        KeyHash = null!;
        RequestFingerprint = null!;
        Claim = null!;
    }

    public ClaimSubmissionIdempotencyRecord(
        string keyHash,
        string requestFingerprint,
        Claim claim,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(claim);

        KeyHash = keyHash;
        RequestFingerprint = requestFingerprint;
        ClaimId = claim.Id;
        Claim = claim;
        CreatedAtUtc = createdAtUtc;
    }

    public string KeyHash { get; private set; }

    public string RequestFingerprint { get; private set; }

    public Guid ClaimId { get; private set; }

    public Claim Claim { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
