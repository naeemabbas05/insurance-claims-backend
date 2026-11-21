using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;
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
                "Connection string 'ClaimsDatabase' is not configured.");

        services.AddDbContext<ClaimsManagementDbContext>(options =>
            options.UseSqlServer(connectionString));

        return services;
    }
}
