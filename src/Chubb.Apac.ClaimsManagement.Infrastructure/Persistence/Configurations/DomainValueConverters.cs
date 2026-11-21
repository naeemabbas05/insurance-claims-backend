using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal static class DomainValueConverters
{
    public static readonly ValueConverter<ClaimNumber, string> ClaimNumberConverter =
        new(value => value.Value, value => ClaimNumber.Create(value));

    public static readonly ValueConverter<PolicyNumber, string> PolicyNumberConverter =
        new(value => value.Value, value => PolicyNumber.Create(value));

    public static readonly ValueConverter<MarketCode, string> MarketCodeConverter =
        new(value => value.Value, value => MarketCode.Create(value));

    public static readonly ValueConverter<CurrencyCode, string> CurrencyCodeConverter =
        new(value => value.Value, value => CurrencyCode.Create(value));

    public static readonly ValueConverter<PersonName, string> PersonNameConverter =
        new(value => value.FullName, value => PersonName.Create(value));

    public static readonly ValueConverter<EmailAddress, string> EmailAddressConverter =
        new(value => value.Value, value => EmailAddress.Create(value));

    public static readonly ValueConverter<PhoneNumber, string> PhoneNumberConverter =
        new(value => value.Value, value => PhoneNumber.Create(value));

    public static readonly ValueConverter<EmailAddress?, string?> NullableEmailAddressConverter =
        new(
            value => value == null ? null : value.Value,
            value => value == null ? null : EmailAddress.Create(value));

    public static readonly ValueConverter<PhoneNumber?, string?> NullablePhoneNumberConverter =
        new(
            value => value == null ? null : value.Value,
            value => value == null ? null : PhoneNumber.Create(value));
}
