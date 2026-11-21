using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class Policy : Entity
{
    private readonly List<Claim> _claims = [];

    private Policy()
    {
        Number = null!;
        Market = null!;
        MarketCode = null!;
        Type = default;
        PolicyHolderName = null!;
        CoveragePeriod = null!;
        CoverageLimit = null!;
    }

    private Policy(
        PolicyNumber number,
        MarketDefinition market,
        PolicyType type,
        PersonName policyHolderName,
        DateRange coveragePeriod,
        Money coverageLimit,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(number);
        ArgumentNullException.ThrowIfNull(market);
        ArgumentNullException.ThrowIfNull(policyHolderName);
        ArgumentNullException.ThrowIfNull(coveragePeriod);
        ArgumentNullException.ThrowIfNull(coverageLimit);
        DomainRule.Require(Enum.IsDefined(type), "Policy type is invalid.");
        DomainRule.Require(coverageLimit.Amount > 0, "Policy coverage limit must be greater than zero.");
        DomainRule.Require(
            coverageLimit.Currency == market.DefaultCurrency,
            "Policy coverage limit must use the market's configured currency.");

        Number = number;
        Market = market;
        MarketCode = market.Code;
        Type = type;
        PolicyHolderName = policyHolderName;
        CoveragePeriod = coveragePeriod;
        CoverageLimit = coverageLimit;
        CreatedAtUtc = DomainRule.Utc(createdAtUtc, nameof(createdAtUtc));
        Status = PolicyStatus.Active;
    }

    public PolicyNumber Number { get; private set; }

    public MarketDefinition Market { get; private set; }

    public MarketCode MarketCode { get; private set; }

    public PolicyType Type { get; private set; }

    public PersonName PolicyHolderName { get; private set; }

    public DateRange CoveragePeriod { get; private set; }

    public Money CoverageLimit { get; private set; }

    public PolicyStatus Status { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? CancelledAtUtc { get; private set; }

    public IReadOnlyCollection<Claim> Claims => _claims.AsReadOnly();

    public static Policy Create(
        PolicyNumber number,
        MarketCatalog marketCatalog,
        MarketCode marketCode,
        PolicyType type,
        PersonName policyHolderName,
        DateRange coveragePeriod,
        Money coverageLimit,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(marketCatalog);
        ArgumentNullException.ThrowIfNull(marketCode);

        return new Policy(
            number,
            marketCatalog.Get(marketCode),
            type,
            policyHolderName,
            coveragePeriod,
            coverageLimit,
            createdAtUtc);
    }

    public bool IsInForceAt(DateTimeOffset incidentAtUtc) =>
        Status == PolicyStatus.Active && CoveragePeriod.Contains(incidentAtUtc);

    public void Cancel(DateTimeOffset cancelledAtUtc)
    {
        DomainRule.Require(Status == PolicyStatus.Active, "Only an active policy can be cancelled.");
        DomainRule.Utc(cancelledAtUtc, nameof(cancelledAtUtc));
        DomainRule.Require(cancelledAtUtc >= CreatedAtUtc, "Policy cancellation cannot precede policy creation.");
        Status = PolicyStatus.Cancelled;
        CancelledAtUtc = cancelledAtUtc;
    }

    internal void AttachClaim(Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claim);
        DomainRule.Require(claim.PolicyId == Id, "Claim does not belong to this policy.");
        DomainRule.Require(_claims.All(existing => existing.Id != claim.Id), "Claim is already attached to this policy.");
        DomainRule.Require(
            _claims.All(existing => existing.Number != claim.Number),
            "Claim number is already in use for this policy.");
        _claims.Add(claim);
    }
}
