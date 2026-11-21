namespace Chubb.Apac.ClaimsManagement.Application.Claims.Submit;

public interface IClaimSubmissionService
{
    Task<ClaimSubmissionResult> SubmitAsync(
        SubmitClaimCommand command,
        CancellationToken cancellationToken);

    Task<ClaimTrackingResult?> GetTrackingAsync(
        string claimReference,
        CancellationToken cancellationToken);
}
