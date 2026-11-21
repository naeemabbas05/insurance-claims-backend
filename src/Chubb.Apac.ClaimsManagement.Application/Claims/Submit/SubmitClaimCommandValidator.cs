using System.Globalization;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public sealed class SubmitClaimCommandValidator : AbstractValidator<SubmitClaimCommand>
{
    private const decimal MaximumFinancialAmount = 999_999_999_999_999.9999m;

    public SubmitClaimCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.IdempotencyKey)
            .NotEmpty()
            .Length(8, 128)
            .Must(key =>
                !string.IsNullOrEmpty(key) &&
                key == key.Trim() &&
                key.All(character => character is >= '!' and <= '~'))
            .WithMessage("Idempotency-Key must contain 8 to 128 visible ASCII characters with no surrounding whitespace.");

        RuleFor(command => command.MarketCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$")
            .WithMessage("Market code must be a two-letter ISO country code.");

        RuleFor(command => command.PolicyNumber)
            .NotEmpty()
            .Length(4, 64)
            .Matches("^[A-Za-z0-9_-]+$")
            .WithMessage("Policy number must contain only letters, digits, hyphens, or underscores.");

        RuleFor(command => command.ClaimType)
            .Must(value => value.HasValue && Enum.IsDefined(value.Value))
            .WithMessage("ClaimType is invalid.");

        RuleFor(command => command.IncidentAtUtc)
            .Must(value => value != default && value.Offset == TimeSpan.Zero)
            .WithMessage("IncidentAtUtc must be a valid UTC timestamp.")
            .Must(value => value <= timeProvider.GetUtcNow())
            .WithMessage("IncidentAtUtc cannot be in the future.");

        RuleFor(command => command.IncidentDescription)
            .NotEmpty()
            .MaximumLength(4000);

        RuleFor(command => command.ClaimantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(command => command.ClaimantEmail)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsValidEmail(value))
            .WithMessage("ClaimantEmail must be a valid email address.");

        RuleFor(command => command.ClaimantPhone)
            .Must(value => string.IsNullOrWhiteSpace(value) || IsValidPhoneNumber(value))
            .WithMessage("ClaimantPhone must contain 6 to 20 digits and standard phone punctuation.");

        RuleFor(command => command)
            .Must(command =>
                !string.IsNullOrWhiteSpace(command.ClaimantEmail) ||
                !string.IsNullOrWhiteSpace(command.ClaimantPhone))
            .WithMessage("At least one claimant contact method is required.");

        RuleFor(command => command.EstimatedLossAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaximumFinancialAmount)
            .WithMessage($"Estimated loss must be between zero and {MaximumFinancialAmount.ToString(CultureInfo.InvariantCulture)}.");

        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$")
            .WithMessage("CurrencyCode must be a three-letter ISO 4217 code.");
    }

    private static bool IsValidEmail(string value)
    {
        var separator = value.IndexOf('@');
        return separator > 0 &&
               separator == value.LastIndexOf('@') &&
               separator < value.Length - 1 &&
               !value.Any(char.IsWhiteSpace) &&
               value[(separator + 1)..].Contains('.');
    }

    private static bool IsValidPhoneNumber(string value)
    {
        var digits = value.Count(char.IsDigit);
        return digits is >= 6 and <= 20 &&
               value.All(character =>
                   char.IsDigit(character) || character is '+' or '-' or '(' or ')' or ' ') &&
               (!value.Contains('+') || value.StartsWith('+'));
    }
}
