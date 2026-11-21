using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class Claimant : Entity
{
    private Claimant()
    {
        Details = null!;
    }

    internal Claimant(Guid claimId, ClaimantDetails details, DateTimeOffset createdAtUtc)
    {
        DomainRule.Require(claimId != Guid.Empty, "Claim identifier cannot be empty.");
        ArgumentNullException.ThrowIfNull(details);

        ClaimId = claimId;
        Details = details;
        CreatedAtUtc = DomainRule.Utc(createdAtUtc, nameof(createdAtUtc));
    }

    public Guid ClaimId { get; private set; }

    public ClaimantDetails Details { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }
}
