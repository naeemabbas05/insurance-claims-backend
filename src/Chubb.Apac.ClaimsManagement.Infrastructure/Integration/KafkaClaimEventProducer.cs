using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal sealed class KafkaClaimEventProducer(IOptions<KafkaOptions> options)
    : IClaimEventKafkaProducer, IDisposable
{
    private readonly object _producerLock = new();
    private IProducer<string, string>? _producer;

    public Task PublishAsync(
        LeasedClaimIntegrationEvent integrationEvent,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var kafkaOptions = options.Value;
        if (string.IsNullOrWhiteSpace(kafkaOptions.BootstrapServers))
        {
            throw new KafkaUnavailableException(
                "Kafka:BootstrapServers is not configured; outbox events remain queued.");
        }

        var message = new Message<string, string>
        {
            Key = integrationEvent.ClaimReference,
            Value = integrationEvent.Payload,
            Headers =
            [
                new Header("event-id", Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString("D"))),
                new Header("event-type", Encoding.UTF8.GetBytes(integrationEvent.EventType))
            ]
        };
        return GetProducer(kafkaOptions.BootstrapServers)
            .ProduceAsync(kafkaOptions.ClaimEventsTopic, message, cancellationToken);
    }

    public void Dispose()
    {
        _producer?.Dispose();
    }

    private IProducer<string, string> GetProducer(string bootstrapServers)
    {
        if (_producer is not null)
        {
            return _producer;
        }

        lock (_producerLock)
        {
            if (_producer is not null)
            {
                return _producer;
            }

            try
            {
                _producer = new ProducerBuilder<string, string>(new ProducerConfig
                {
                    BootstrapServers = bootstrapServers,
                    Acks = Acks.All,
                    EnableIdempotence = true,
                    MessageTimeoutMs = 30_000
                }).Build();
            }
            catch (ArgumentException exception)
            {
                throw new KafkaUnavailableException(
                    "Kafka:BootstrapServers is invalid; outbox events remain queued.",
                    exception);
            }

            return _producer;
        }
    }
}
