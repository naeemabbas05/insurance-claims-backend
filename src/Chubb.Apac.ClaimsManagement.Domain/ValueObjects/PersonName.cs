using Chubb.Apac.ClaimsManagement.Domain.Common;

namespace Chubb.Apac.ClaimsManagement.Domain.ValueObjects;

public sealed record PersonName
{
    private PersonName(string fullName)
    {
        FullName = fullName;
    }

    public string FullName { get; }

    public static PersonName Create(string fullName) =>
        new(DomainRule.RequiredText(fullName, nameof(fullName), 200));
}
