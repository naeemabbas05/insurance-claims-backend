namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public interface IManagementClaimsReportingService
{
    Task<ManagementClaimsReportResult> GetClaimsReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken);
}
