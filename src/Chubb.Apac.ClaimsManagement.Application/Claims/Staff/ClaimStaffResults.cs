using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record StaffClaimListItem(
    string ClaimReference,
    string PolicyNumber,
    string MarketCode,
    ClaimType ClaimType,
    ClaimStatus Status,
    DateTimeOffset IncidentAtUtc,
    DateTimeOffset ReportedAtUtc,
    AssignmentRole? ActiveRole,
    Guid? ActiveOfficerId,
    string? ActiveOfficerName);

public sealed record ClaimAssignmentHistoryItem(
    Guid AssignmentId,
    string ClaimReference,
    Guid OfficerId,
    string OfficerName,
    AssignmentRole Role,
    DateTimeOffset AssignedAtUtc,
    DateTimeOffset? UnassignedAtUtc,
    bool IsActive);

public sealed record OfficerWorkloadResult(
    Guid OfficerId,
    string OfficerName,
    string MarketCode,
    int ActiveClaimCount,
    IReadOnlyList<AssignmentRoleWorkload> ByRole);

public sealed record AssignmentRoleWorkload(AssignmentRole Role, int ActiveClaimCount);

public sealed record ClaimAssignmentResult(
    Guid AssignmentId,
    string ClaimReference,
    Guid OfficerId,
    AssignmentRole Role,
    DateTimeOffset AssignedAtUtc,
    Guid AssignmentActorId);

public sealed record StaffClaimPage(
    IReadOnlyList<Chubb.Apac.ClaimsManagement.Domain.Entities.Claim> Claims,
    int TotalCount);

public sealed record StaffAssignmentWorkload(
    int ActiveClaimCount,
    IReadOnlyList<AssignmentRoleWorkload> ByRole);
