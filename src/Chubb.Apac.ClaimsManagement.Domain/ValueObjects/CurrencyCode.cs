using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record CurrencyCode
{
    private CurrencyCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static CurrencyCode Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 3).ToUpperInvariant();
        DomainRule.Require(
            normalized.Length == 3 && normalized.All(character => character is >= 'A' and <= 'Z'),
            "Currency code must be a three-letter ISO 4217 code.");

        return new CurrencyCode(normalized);
    }

    public override string ToString() => Value;
}
