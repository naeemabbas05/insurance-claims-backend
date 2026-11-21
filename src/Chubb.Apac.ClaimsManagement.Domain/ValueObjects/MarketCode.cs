using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record MarketCode
{
    private MarketCode(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static MarketCode Create(string value)
    {
        var normalized = DomainRule.RequiredText(value, nameof(value), 2).ToUpperInvariant();
        DomainRule.Require(
            normalized.Length == 2 && normalized.All(character => character is >= 'A' and <= 'Z'),
            "Market code must be a two-letter ISO country code.");

        return new MarketCode(normalized);
    }

    public override string ToString() => Value;
}
