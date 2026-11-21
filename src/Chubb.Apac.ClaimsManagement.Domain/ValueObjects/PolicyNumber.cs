using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record PolicyNumber
{
    private PolicyNumber(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PolicyNumber Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 64).ToUpperInvariant();
        DomainRule.Require(
            normalized.Length >= 4 &&
            normalized.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '-' or '_'),
            "Policy number must contain at least four letters, digits, hyphens, or underscores.");
        return new PolicyNumber(normalized);
    }
}
