using Chubb.Apac.ClaimsManagement.Domain.Enums;

namespace Chubb.Apac.ClaimsManagement.Api.Models;

public sealed record AssignClaimRequest(Guid OfficerId, AssignmentRole? Role);

public sealed record ReassignClaimRequest(Guid NewOfficerId);
