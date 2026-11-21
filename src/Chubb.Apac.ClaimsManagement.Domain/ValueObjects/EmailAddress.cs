using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record EmailAddress
{
    private EmailAddress(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static EmailAddress Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 254);
        var separator = normalized.IndexOf('@');
        DomainRule.Require(
            separator > 0 &&
            separator == normalized.LastIndexOf('@') &&
            separator < normalized.Length - 1 &&
            !normalized.Any(char.IsWhiteSpace) &&
            normalized[(separator + 1)..].Contains('.'),
            "Email address is invalid.");

        return new EmailAddress(normalized);
    }
}
