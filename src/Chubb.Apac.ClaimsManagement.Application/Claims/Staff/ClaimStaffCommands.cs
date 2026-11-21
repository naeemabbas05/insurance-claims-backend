using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public interface IStaffMarketQuery
{
    Guid ActorStaffUserId { get; }

    string MarketCode { get; }
}

public interface IStaffMarketPageQuery : IStaffMarketQuery
{
    int Page { get; }

    int PageSize { get; }
}

public sealed record ClaimMarketPageQuery(
    Guid ActorStaffUserId,
    string MarketCode,
    int Page = 1,
    int PageSize = 20) : IStaffMarketPageQuery;

public sealed record OfficerClaimsPageQuery(
    Guid ActorStaffUserId,
    Guid OfficerId,
    string MarketCode,
    int Page = 1,
    int PageSize = 20) : IStaffMarketPageQuery;

public sealed record OfficerWorkloadQuery(
    Guid ActorStaffUserId,
    Guid OfficerId,
    string MarketCode) : IStaffMarketQuery;

public sealed record ClaimAssignmentHistoryQuery(
    Guid ActorStaffUserId,
    string ClaimReference);

public sealed record AssignClaimCommand(
    Guid ActorStaffUserId,
    string ClaimReference,
    Guid OfficerId,
    AssignmentRole? Role);

public sealed record ReassignClaimCommand(
    Guid ActorStaffUserId,
    string ClaimReference,
    Guid AssignmentId,
    Guid NewOfficerId);
