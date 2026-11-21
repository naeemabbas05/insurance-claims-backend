namespace Chubb.Apac.ClaimsManagement.Application.Security;

public interface IClaimAccessAuthorizationService
{
    Task AuthorizeStaffAsync(
        Guid staffUserId,
        ClaimAccessRole role,
        string marketCode,
        CancellationToken cancellationToken);

    Task<ClaimAccessResult> AuthorizeAsync(
        ClaimAccessQuery query,
        CancellationToken cancellationToken);
}
