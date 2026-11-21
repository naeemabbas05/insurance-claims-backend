using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record PhoneNumber
{
    private PhoneNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PhoneNumber Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 30);
        var digits = normalized.Where(char.IsDigit).ToArray();
        DomainRule.Require(
            digits.Length is >= 6 and <= 20 &&
            normalized.All(character =>
                char.IsDigit(character) || character is '+' or '-' or '(' or ')' or ' '),
            "Phone number must contain 6 to 20 digits and only standard phone punctuation.");
        DomainRule.Require(
            !normalized.Contains('+') || normalized.StartsWith('+'),
            "A plus sign is only valid at the start of a phone number.");

        return new PhoneNumber(normalized);
    }
}
