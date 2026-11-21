using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record ContactDetails
{
    private ContactDetails(EmailAddress? email, PhoneNumber? phone)
    {
        Email = email;
        Phone = phone;
    }

    public EmailAddress? Email { get; }

    public PhoneNumber? Phone { get; }

    public static ContactDetails Create(EmailAddress? email, PhoneNumber? phone)
    {
        DomainRule.Require(email is not null || phone is not null, "At least one contact method is required.");
        return new ContactDetails(email, phone);
    }

    public static ContactDetails Create(string? email, string? phone) =>
        Create(
            string.IsNullOrWhiteSpace(email) ? null : EmailAddress.Create(email),
            string.IsNullOrWhiteSpace(phone) ? null : PhoneNumber.Create(phone));
}
