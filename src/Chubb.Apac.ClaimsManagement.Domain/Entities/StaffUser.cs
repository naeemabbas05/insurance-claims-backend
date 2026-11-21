using Chubb.Apac.ClaimsManagement.Domain.Common;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

namespace Chubb.Apac.ClaimsManagement.Domain.Entities;

public sealed class StaffUser : Entity
{
    private readonly List<MarketCode> _markets = [];

    private StaffUser()
    {
        Name = null!;
        Email = null!;
    }

    private StaffUser(
        PersonName name,
        EmailAddress email,
        StaffRole role,
        IEnumerable<MarketCode> markets,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        DomainRule.Require(Enum.IsDefined(role), "Staff role is invalid.");

        Name = name;
        Email = email;
        Role = role;
        CreatedAtUtc = DomainRule.Utc(createdAtUtc, nameof(createdAtUtc));
        _markets.AddRange(markets);
    }

    public PersonName Name { get; private set; }

    public EmailAddress Email { get; private set; }

    public StaffRole Role { get; private set; }

    public IReadOnlyCollection<MarketCode> Markets => _markets.AsReadOnly();

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? DeactivatedAtUtc { get; private set; }

    public bool IsActive => DeactivatedAtUtc is null;

    public static StaffUser Create(
        PersonName name,
        EmailAddress email,
        StaffRole role,
        IEnumerable<MarketCode> markets,
        MarketCatalog marketCatalog,
        DateTimeOffset createdAtUtc)
    {
        ArgumentNullException.ThrowIfNull(markets);
        ArgumentNullException.ThrowIfNull(marketCatalog);

        var configuredMarkets = markets.Distinct().ToArray();
        DomainRule.Require(configuredMarkets.Length > 0, "Staff user must be assigned to at least one market.");
        foreach (var market in configuredMarkets)
        {
            marketCatalog.Get(market);
        }

        return new StaffUser(name, email, role, configuredMarkets, createdAtUtc);
    }

    public bool CanWorkIn(MarketCode market) =>
        IsActive && _markets.Contains(market);

    public bool CanPerform(StaffRole requiredRole) =>
        IsActive && (Role == StaffRole.Administrator || Role == requiredRole);

    public void Deactivate(DateTimeOffset deactivatedAtUtc)
    {
        DomainRule.Require(IsActive, "Staff user is already deactivated.");
        DomainRule.Utc(deactivatedAtUtc, nameof(deactivatedAtUtc));
        DomainRule.Require(deactivatedAtUtc >= CreatedAtUtc, "Deactivation cannot precede account creation.");
        DeactivatedAtUtc = deactivatedAtUtc;
    }
}
