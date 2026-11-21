using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record MarketDefinition
{
    private MarketDefinition()
    {
        Code = null!;
        Name = null!;
        DefaultCurrency = null!;
    }

    private MarketDefinition(MarketCode code, string name, CurrencyCode defaultCurrency)
    {
        Code = code;
        Name = name;
        DefaultCurrency = defaultCurrency;
    }

    public MarketCode Code { get; private set; }

    public string Name { get; private set; }

    public CurrencyCode DefaultCurrency { get; private set; }

    public static MarketDefinition Create(string code, string name, string defaultCurrency) =>
        new(
            MarketCode.Create(code),
            DomainRule.RequiredText(name, nameof(name), 100),
            CurrencyCode.Create(defaultCurrency));
}
