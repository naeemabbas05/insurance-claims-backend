using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Chubb.Apac.ClaimsManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("ClaimsDatabase")
            ?? throw new InvalidOperationException(
                "Connection string 'ClaimsDatabase' is not configured. Set the ConnectionStrings__ClaimsDatabase environment variable.");

        services.AddDbContext<ClaimsDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IClaimSubmissionRepository, ClaimSubmissionRepository>();
        services.AddScoped<IClaimLifecycleRepository, ClaimLifecycleRepository>();

        return services;
    }
}
