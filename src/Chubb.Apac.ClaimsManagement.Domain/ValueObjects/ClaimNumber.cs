using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record ClaimNumber
{
    private ClaimNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static ClaimNumber Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 40).ToUpperInvariant();
        DomainRule.Require(
            normalized.Length >= 4 &&
            normalized.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_'),
            "Claim number must contain at least four letters, digits, hyphens, or underscores.");
        return new ClaimNumber(normalized);
    }
}
