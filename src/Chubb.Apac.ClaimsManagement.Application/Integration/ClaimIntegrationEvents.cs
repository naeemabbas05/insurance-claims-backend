namespace Chubb.Apac.ClaimsManagement.Application.Integration;

public abstract record ClaimIntegrationEvent(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode)
{
    public int SchemaVersion => 1;

    public abstract string EventType { get; }
}

public sealed record ClaimSubmittedV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    Guid ClaimantId,
    string ClaimType,
    decimal EstimatedLossAmount,
    string CurrencyCode)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimSubmitted";
}

public sealed record ClaimAssignedV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    Guid AssignmentId,
    Guid StaffUserId,
    string AssignmentRole)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimAssigned";
}

public sealed record ClaimStatusChangedV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    string PreviousStatus,
    string NewStatus,
    Guid? ActorId)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimStatusChanged";
}

public sealed record ClaimApprovedV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    Guid ApprovedByStaffUserId,
    decimal ApprovedAmount,
    string CurrencyCode)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimApproved";
}

public sealed record ClaimRejectedV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    Guid RejectedByStaffUserId)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimRejected";
}

public sealed record ClaimSettledV1(
    Guid EventId,
    DateTimeOffset OccurredAtUtc,
    Guid ClaimId,
    string ClaimReference,
    string MarketCode,
    Guid SettledByStaffUserId,
    decimal TotalPaidSettlementAmount,
    string CurrencyCode)
    : ClaimIntegrationEvent(EventId, OccurredAtUtc, ClaimId, ClaimReference, MarketCode)
{
    public override string EventType => "ClaimSettled";
}
