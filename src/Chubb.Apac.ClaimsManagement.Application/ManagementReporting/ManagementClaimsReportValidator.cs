using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public sealed class ManagementClaimsReportValidator : AbstractValidator<ManagementClaimsReportQuery>
{
    public ManagementClaimsReportValidator()
    {
        RuleFor(query => query.ActorStaffUserId).NotEmpty();
        RuleFor(query => query.MarketCode)
            .Must(marketCode => marketCode is null ||
                System.Text.RegularExpressions.Regex.IsMatch(marketCode, "^[A-Za-z]{2}$"))
            .WithMessage("Market code must be a two-letter code.");
        RuleFor(query => query.ToDate)
            .Must((query, toDate) => !query.FromDate.HasValue ||
                !toDate.HasValue ||
                toDate.Value >= query.FromDate.Value)
            .WithMessage("ToDate must be on or after FromDate.");
    }
}
