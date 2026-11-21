using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;

public sealed class ClaimantNotificationQueryValidator : AbstractValidator<ClaimantNotificationQuery>
{
    public ClaimantNotificationQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query)
            .Must(query => (long)(query.Page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("The requested page is too large.");
        RuleFor(query => query)
            .Must(query =>
                query.ClaimantId.HasValue ||
                (query.EmailVerified && !string.IsNullOrWhiteSpace(query.Email)))
            .WithMessage("A claimant identity or verified email is required.");
    }
}
