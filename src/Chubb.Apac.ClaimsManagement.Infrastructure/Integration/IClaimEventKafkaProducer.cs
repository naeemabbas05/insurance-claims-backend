namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal interface IClaimEventKafkaProducer
{
    Task PublishAsync(
        LeasedClaimIntegrationEvent integrationEvent,
        CancellationToken cancellationToken);
}
