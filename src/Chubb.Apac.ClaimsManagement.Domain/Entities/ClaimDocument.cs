using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimDocument : Entity
{
    private ClaimDocument()
    {
        Metadata = null!;
    }

    internal ClaimDocument(
        Guid claimId,
        ClaimDocumentType type,
        ClaimDocumentMetadata metadata,
        Guid? uploadedByClaimantId,
        Guid? uploadedByStaffUserId,
        DateTimeOffset uploadedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(Enum.IsDefined(type), "Document type is invalid.");
        DomainRule.Require(
            uploadedByClaimantId.HasValue != uploadedByStaffUserId.HasValue,
            "Exactly one claimant or staff uploader must be specified.");

        ClaimId = claimId;
        Type = type;
        Metadata = metadata;
        UploadedByClaimantId = uploadedByClaimantId;
        UploadedByStaffUserId = uploadedByStaffUserId;
        UploadedAtUtc = DomainRule.Utc(uploadedAtUtc, nameof(uploadedAtUtc));
    }

    public Guid ClaimId { get; private set; }

    public ClaimDocumentType Type { get; private set; }

    public ClaimDocumentMetadata Metadata { get; private set; }

    public Guid? UploadedByClaimantId { get; private set; }

    public Guid? UploadedByStaffUserId { get; private set; }

    public DateTimeOffset UploadedAtUtc { get; private set; }
}
