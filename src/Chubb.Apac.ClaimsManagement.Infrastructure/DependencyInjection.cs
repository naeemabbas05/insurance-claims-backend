using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;
using Chubb.Apac.ClaimsManagement.Application.Claims.Submit;
using Chubb.Apac.ClaimsManagement.Application.Claims.Lifecycle;
using Chubb.Apac.ClaimsManagement.Application.Claims.Staff;
using Chubb.Apac.ClaimsManagement.Application.ManagementReporting;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Infrastructure.Integration;
using Chubb.Apac.ClaimsManagement.Application.Security;
using Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;
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
        services.AddScoped<IClaimStaffOperationsRepository, ClaimStaffOperationsRepository>();
        services.AddScoped<IManagementClaimsReportRepository, ManagementClaimsReportRepository>();
        services.AddScoped<IClaimAccessRepository, ClaimAccessRepository>();
        services.AddScoped<IClaimantNotificationRepository, ClaimantNotificationRepository>();
        services.AddScoped<IClaimIntegrationEventOutbox, EfCoreClaimIntegrationEventOutbox>();
        services.AddScoped<IClaimOutboxStore, ClaimOutboxStore>();
        services.Configure<KafkaOptions>(options =>
        {
            options.BootstrapServers = configuration[$"{KafkaOptions.SectionName}:BootstrapServers"];
            options.ClaimEventsTopic =
                configuration[$"{KafkaOptions.SectionName}:ClaimEventsTopic"] ?? "claims.events";
        });
        services.AddSingleton<IClaimEventKafkaProducer, KafkaClaimEventProducer>();
        services.AddHostedService<ClaimOutboxPublisherBackgroundService>();

        return services;
    }
}
