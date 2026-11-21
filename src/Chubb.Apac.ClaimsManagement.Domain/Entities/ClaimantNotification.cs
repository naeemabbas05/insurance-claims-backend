using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class ClaimantNotification : Entity
{
    private ClaimantNotification()
    {
        Claim = null!;
        Claimant = null!;
        Category = null!;
        Title = null!;
        Message = null!;
        DeduplicationKey = null!;
    }

    internal ClaimantNotification(
        Claim claim,
        Claimant claimant,
        string category,
        string title,
        string message,
        DateTimeOffset createdAtUtc,
        string deduplicationKey)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(claimant);
        DomainRule.Require(claim.Id != Guid.Empty, "Claim identifier cannot be empty.");
        DomainRule.Require(claimant.ClaimId == claim.Id, "Notification recipient must belong to the claim.");

        ClaimId = claim.Id;
        Claim = claim;
        ClaimantId = claimant.Id;
        Claimant = claimant;
        Category = DomainRule.RequiredText(category, nameof(category), 64);
        Title = DomainRule.RequiredText(title, nameof(title), 160);
        Message = DomainRule.RequiredText(message, nameof(message), 1000);
        CreatedAtUtc = DomainRule.Utc(createdAtUtc, nameof(createdAtUtc));
        DeduplicationKey = DomainRule.RequiredText(deduplicationKey, nameof(deduplicationKey), 160);
    }

    public Guid ClaimId { get; private set; }

    public Claim Claim { get; private set; }

    public Guid ClaimantId { get; private set; }

    public Claimant Claimant { get; private set; }

    public string Category { get; private set; }

    public string Title { get; private set; }

    public string Message { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? ReadAtUtc { get; private set; }

    public string DeduplicationKey { get; private set; }

    public void MarkRead(DateTimeOffset readAtUtc)
    {
        if (ReadAtUtc.HasValue)
        {
            return;
        }

        var readAt = DomainRule.Utc(readAtUtc, nameof(readAtUtc));
        DomainRule.Require(readAt >= CreatedAtUtc, "Notification cannot be read before it was created.");
        ReadAtUtc = readAt;
    }
}
