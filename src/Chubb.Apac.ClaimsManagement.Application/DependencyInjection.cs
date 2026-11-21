using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
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
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        return services;
    }
}
