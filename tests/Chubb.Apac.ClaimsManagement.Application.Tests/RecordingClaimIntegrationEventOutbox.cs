using Chubb.Apac.ClaimsManagement.Application.Integration;

namespace Chubb.Apac.ClaimsManagement.Application.Tests;

internal sealed class RecordingClaimIntegrationEventOutbox : IClaimIntegrationEventOutbox
{
    private readonly List<ClaimIntegrationEvent> _events = [];

    public IReadOnlyList<ClaimIntegrationEvent> Events => _events;

    public void Enqueue(ClaimIntegrationEvent integrationEvent) =>
        _events.Add(integrationEvent);
}
