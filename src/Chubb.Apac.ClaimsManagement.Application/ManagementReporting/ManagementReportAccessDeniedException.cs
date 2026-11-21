namespace Chubb.Apac.ClaimsManagement.Application.ManagementReporting;

public sealed class ManagementReportAccessDeniedException()
    : Exception("The current staff user is not authorized to view management reports.");
