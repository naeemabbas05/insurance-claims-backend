namespace Chubb.Apac.ClaimsManagement.Application.Claims.Staff;

public interface IClaimStaffOperationsService
{
    Task<PagedResult<StaffClaimListItem>> GetIncomingClaimsAsync(
        ClaimMarketPageQuery query,
        CancellationToken cancellationToken);

    Task<PagedResult<StaffClaimListItem>> GetUnassignedClaimsAsync(
        ClaimMarketPageQuery query,
        CancellationToken cancellationToken);

    Task<PagedResult<StaffClaimListItem>> GetOfficerClaimsAsync(
        OfficerClaimsPageQuery query,
        CancellationToken cancellationToken);

    Task<OfficerWorkloadResult> GetOfficerWorkloadAsync(
        OfficerWorkloadQuery query,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ClaimAssignmentHistoryItem>> GetAssignmentHistoryAsync(
        ClaimAssignmentHistoryQuery query,
        CancellationToken cancellationToken);

    Task<ClaimAssignmentResult> AssignAsync(
        AssignClaimCommand command,
        CancellationToken cancellationToken);

    Task<ClaimAssignmentResult> ReassignAsync(
        ReassignClaimCommand command,
        CancellationToken cancellationToken);
}
