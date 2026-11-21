namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string? BootstrapServers { get; set; }

    public string ClaimEventsTopic { get; set; } = "claims.events";
}
