using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed class MarketCatalog
{
    public const int SupportedMarketCount = 6;

    private readonly Dictionary<MarketCode, MarketDefinition> _markets;
    private readonly IReadOnlyCollection<MarketDefinition> _definitions;

    private MarketCatalog(IEnumerable<MarketDefinition> definitions)
    {
        var configuredMarkets = definitions.ToArray();
        DomainRule.Require(
            configuredMarkets.Length == SupportedMarketCount,
            $"Exactly {SupportedMarketCount} APAC market definitions are required.");
        DomainRule.Require(
            configuredMarkets.Select(market => market.Code).Distinct().Count() == configuredMarkets.Length,
            "Market codes must be unique.");

        _markets = configuredMarkets.ToDictionary(market => market.Code);
        _definitions = Array.AsReadOnly(configuredMarkets);
    }

    public static MarketCatalog Default { get; } = Create(
    [
        MarketDefinition.Create("MY", "Malaysia", "MYR"),
        MarketDefinition.Create("ID", "Indonesia", "IDR"),
        MarketDefinition.Create("SG", "Singapore", "SGD"),
        MarketDefinition.Create("KH", "Cambodia", "KHR"),
        MarketDefinition.Create("TH", "Thailand", "THB"),
        MarketDefinition.Create("AU", "Australia", "AUD")
    ]);

    public IReadOnlyCollection<MarketDefinition> Markets => _definitions;

    public static MarketCatalog Create(IEnumerable<MarketDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        return new MarketCatalog(definitions);
    }

    public MarketDefinition Get(MarketCode code)
    {
        ArgumentNullException.ThrowIfNull(code);
        return _markets.TryGetValue(code, out var market)
            ? market
            : throw new DomainRuleViolationException($"Market '{code}' is not configured.");
    }
}
