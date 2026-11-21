using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Chubb.Apac.ClaimsManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<SubmitClaimCommandValidator>();
        services.AddScoped<IClaimSubmissionService, ClaimSubmissionService>();
        services.AddScoped<IClaimLifecycleService, ClaimLifecycleService>();
        services.AddScoped<IClaimStaffOperationsService, ClaimStaffOperationsService>();
        services.AddScoped<IManagementClaimsReportingService, ManagementClaimsReportingService>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        return services;
    }
}
