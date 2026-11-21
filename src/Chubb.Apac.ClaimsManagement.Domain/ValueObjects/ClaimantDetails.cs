using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record ClaimantDetails
{
    private ClaimantDetails()
    {
        Name = null!;
        Contact = null!;
    }

    private ClaimantDetails(ClaimantRole role, PersonName name, ContactDetails contact)
    {
        Role = role;
        Name = name;
        Contact = contact;
    }

    public ClaimantRole Role { get; private set; }

    public PersonName Name { get; private set; }

    public ContactDetails Contact { get; private set; }

    public static ClaimantDetails Create(
        ClaimantRole role,
        PersonName name,
        ContactDetails contact)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(contact);
        DomainRule.Require(Enum.IsDefined(role), "Claimant role is invalid.");
        return new ClaimantDetails(role, name, contact);
    }
}
