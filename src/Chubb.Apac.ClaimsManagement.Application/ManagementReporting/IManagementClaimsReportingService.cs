namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public interface IManagementClaimsReportingService
{
    Task<ManagementClaimsReportResult> GetClaimsReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken);

    Task<ManagementClaimsAgingReportResult> GetAgingReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken);

    Task<ManagementClaimsPerformanceReportResult> GetPerformanceReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken);

    Task<ManagementClaimsSlaReportResult> GetSlaReportAsync(
        ManagementClaimsReportQuery query,
        CancellationToken cancellationToken);
}
