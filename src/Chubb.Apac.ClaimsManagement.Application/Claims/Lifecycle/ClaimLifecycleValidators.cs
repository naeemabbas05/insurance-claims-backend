using Chubb.Apac.ClaimsManagement.Domain.Enums;
using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;

public abstract class StaffClaimCommandValidator<TCommand> : AbstractValidator<TCommand>
    where TCommand : IStaffClaimCommand
{
    protected StaffClaimCommandValidator()
    {
        RuleFor(command => command.ClaimReference)
            .NotEmpty()
            .Matches("^CLM-[A-Za-z]{2}-[A-Fa-f0-9]{32}$");
        RuleFor(command => command.StaffUserId).NotEmpty();
    }
}

public sealed class RecordClaimAssessmentCommandValidator
    : StaffClaimCommandValidator<RecordClaimAssessmentCommand>
{
    private const decimal MaximumFinancialAmount = 999_999_999_999_999.9999m;

    public RecordClaimAssessmentCommandValidator()
    {
        RuleFor(command => command.AssessedDamageAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaximumFinancialAmount);
        RuleFor(command => command.RecommendedSettlementAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(command => command.AssessedDamageAmount);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$");
        RuleFor(command => command.Findings)
            .NotEmpty()
            .MaximumLength(4000);
    }
}

public sealed class ApproveClaimCommandValidator : StaffClaimCommandValidator<ApproveClaimCommand>
{
    private const decimal MaximumFinancialAmount = 999_999_999_999_999.9999m;

    public ApproveClaimCommandValidator()
    {
        RuleFor(command => command.ApprovedAmount)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaximumFinancialAmount);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$");
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}

public sealed class RejectClaimCommandValidator : StaffClaimCommandValidator<RejectClaimCommand>
{
    public RejectClaimCommandValidator() =>
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(1000);
}

public sealed class RequestClaimInformationCommandValidator
    : StaffClaimCommandValidator<RequestClaimInformationCommand>
{
    public RequestClaimInformationCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(command => command.ClaimantId).NotEmpty();
        RuleFor(command => command.Question)
            .NotEmpty()
            .MaximumLength(2000);
        RuleFor(command => command.DueAtUtc)
            .Must(value => value.Offset == TimeSpan.Zero)
            .WithMessage("DueAtUtc must be a UTC timestamp.")
            .Must(value => value > timeProvider.GetUtcNow())
            .WithMessage("DueAtUtc must be in the future.");
    }
}

public sealed class RespondToClaimInformationCommandValidator
    : AbstractValidator<RespondToClaimInformationCommand>
{
    public RespondToClaimInformationCommandValidator()
    {
        RuleFor(command => command.ClaimReference)
            .NotEmpty()
            .Matches("^CLM-[A-Za-z]{2}-[A-Fa-f0-9]{32}$");
        RuleFor(command => command.ClaimantId).NotEmpty();
        RuleFor(command => command.RequestId).NotEmpty();
        RuleFor(command => command.Response)
            .NotEmpty()
            .MaximumLength(4000);
    }
}

public sealed class CancelClaimInformationCommandValidator
    : StaffClaimCommandValidator<CancelClaimInformationCommand>
{
    public CancelClaimInformationCommandValidator() =>
        RuleFor(command => command.RequestId).NotEmpty();
}

public sealed class ProposeClaimSettlementCommandValidator
    : StaffClaimCommandValidator<ProposeClaimSettlementCommand>
{
    private const decimal MaximumFinancialAmount = 999_999_999_999_999.9999m;

    public ProposeClaimSettlementCommandValidator()
    {
        RuleFor(command => command.PayeeClaimantId).NotEmpty();
        RuleFor(command => command.Amount)
            .GreaterThan(0)
            .LessThanOrEqualTo(MaximumFinancialAmount);
        RuleFor(command => command.CurrencyCode)
            .NotEmpty()
            .Matches("^[A-Za-z]{3}$");
    }
}

public sealed class ApproveClaimSettlementCommandValidator
    : StaffClaimCommandValidator<ApproveClaimSettlementCommand>
{
    public ApproveClaimSettlementCommandValidator() =>
        RuleFor(command => command.SettlementId).NotEmpty();
}

public sealed class PayClaimSettlementCommandValidator
    : StaffClaimCommandValidator<PayClaimSettlementCommand>
{
    public PayClaimSettlementCommandValidator() =>
        RuleFor(command => command.SettlementId).NotEmpty();
}

public sealed class CancelClaimSettlementCommandValidator
    : StaffClaimCommandValidator<CancelClaimSettlementCommand>
{
    public CancelClaimSettlementCommandValidator() =>
        RuleFor(command => command.SettlementId).NotEmpty();
}

public sealed class ChangeClaimWorkflowStatusCommandValidator
    : StaffClaimCommandValidator<ChangeClaimWorkflowStatusCommand>
{
    public ChangeClaimWorkflowStatusCommandValidator()
    {
        RuleFor(command => command.NewStatus)
            .Must(status => status is ClaimStatus.UnderReview or ClaimStatus.Closed)
            .WithMessage("Only claims may be reopened to UnderReview or transitioned to Closed here.");
        RuleFor(command => command.Reason)
            .NotEmpty()
            .MaximumLength(1000);
    }
}
