using Chubb.Apac.ClaimsManagement.Domain.Enums;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public abstract class StaffMarketPageQueryValidator<TQuery> : AbstractValidator<TQuery>
    where TQuery : IStaffMarketPageQuery
{
    protected StaffMarketPageQueryValidator()
    {
        RuleFor(query => query.ActorStaffUserId).NotEmpty();
        RuleFor(query => query.MarketCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$");
        RuleFor(query => query.Page).InclusiveBetween(1, 1_000_000);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class ClaimMarketPageQueryValidator
    : StaffMarketPageQueryValidator<ClaimMarketPageQuery>
{
}

public sealed class OfficerClaimsPageQueryValidator
    : StaffMarketPageQueryValidator<OfficerClaimsPageQuery>
{
    public OfficerClaimsPageQueryValidator() =>
        RuleFor(query => query.OfficerId).NotEmpty();
}

public sealed class OfficerWorkloadQueryValidator : AbstractValidator<OfficerWorkloadQuery>
{
    public OfficerWorkloadQueryValidator()
    {
        RuleFor(query => query.ActorStaffUserId).NotEmpty();
        RuleFor(query => query.OfficerId).NotEmpty();
        RuleFor(query => query.MarketCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{2}$");
    }
}

public sealed class ClaimAssignmentHistoryQueryValidator
    : AbstractValidator<ClaimAssignmentHistoryQuery>
{
    public ClaimAssignmentHistoryQueryValidator()
    {
        RuleFor(query => query.ActorStaffUserId).NotEmpty();
        RuleFor(query => query.ClaimReference)
            .NotEmpty()
            .Matches("^CLM-[A-Za-z]{2}-[A-Fa-f0-9]{32}$");
    }
}

public sealed class AssignClaimCommandValidator : AbstractValidator<AssignClaimCommand>
{
    public AssignClaimCommandValidator()
    {
        RuleFor(command => command.ActorStaffUserId).NotEmpty();
        RuleFor(command => command.OfficerId).NotEmpty();
        RuleFor(command => command.ClaimReference)
            .NotEmpty()
            .Matches("^CLM-[A-Za-z]{2}-[A-Fa-f0-9]{32}$");
        RuleFor(command => command.Role)
            .Must(role => role.HasValue && Enum.IsDefined(role.Value))
            .WithMessage("Assignment role is invalid.");
    }
}

public sealed class ReassignClaimCommandValidator : AbstractValidator<ReassignClaimCommand>
{
    public ReassignClaimCommandValidator()
    {
        RuleFor(command => command.ActorStaffUserId).NotEmpty();
        RuleFor(command => command.AssignmentId).NotEmpty();
        RuleFor(command => command.NewOfficerId).NotEmpty();
        RuleFor(command => command.ClaimReference)
            .NotEmpty()
            .Matches("^CLM-[A-Za-z]{2}-[A-Fa-f0-9]{32}$");
    }
}
