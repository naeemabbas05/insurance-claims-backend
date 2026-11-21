namespace Chubb.Apac.ClaimsManagement.Application.Integration;

public interface IClaimIntegrationEventOutbox
{
    void Enqueue(ClaimIntegrationEvent integrationEvent);
}
