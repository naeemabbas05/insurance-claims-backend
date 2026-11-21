using System.Text.Json;
using Chubb.Apac.ClaimsManagement.Application.Integration;
using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

public sealed class EfCoreClaimIntegrationEventOutbox(ClaimsDbContext dbContext)
    : IClaimIntegrationEventOutbox
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public void Enqueue(ClaimIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var payload = JsonSerializer.Serialize(
            integrationEvent,
            integrationEvent.GetType(),
            SerializerOptions);
        dbContext.OutboxMessages.Add(OutboxMessage.Create(
            integrationEvent.EventId,
            integrationEvent.EventType,
            integrationEvent.ClaimId,
            integrationEvent.ClaimReference,
            payload,
            integrationEvent.OccurredAtUtc));
    }
}
