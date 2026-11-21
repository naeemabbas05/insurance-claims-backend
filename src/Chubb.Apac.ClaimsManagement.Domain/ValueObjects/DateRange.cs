using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record DateRange
{
    private DateRange(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        StartsAtUtc = startsAtUtc;
        EndsAtUtc = endsAtUtc;
    }

    public DateTimeOffset StartsAtUtc { get; }

    public DateTimeOffset EndsAtUtc { get; }

    public static DateRange Create(DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
    {
        DomainRule.Utc(startsAtUtc, nameof(startsAtUtc));
        DomainRule.Utc(endsAtUtc, nameof(endsAtUtc));
        DomainRule.Require(endsAtUtc > startsAtUtc, "Date range end must be after its start.");
        return new DateRange(startsAtUtc, endsAtUtc);
    }

    public bool Contains(DateTimeOffset instantUtc)
    {
        DomainRule.Utc(instantUtc, nameof(instantUtc));
        return instantUtc >= StartsAtUtc && instantUtc < EndsAtUtc;
    }
}
