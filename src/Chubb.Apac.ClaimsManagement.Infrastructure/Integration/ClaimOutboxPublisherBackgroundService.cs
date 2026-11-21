using System.Data.Common;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal sealed class ClaimOutboxPublisherBackgroundService(
    IServiceScopeFactory scopeFactory,
    IClaimEventKafkaProducer kafkaProducer,
    TimeProvider timeProvider,
    ILogger<ClaimOutboxPublisherBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (DbUpdateException exception)
            {
                logger.LogError(exception, "Could not update the claim event outbox.");
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (DbException exception)
            {
                logger.LogError(exception, "Could not read the claim event outbox.");
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OutboxLeaseLostException exception)
            {
                logger.LogWarning(exception, "Claim event outbox lease expired while publishing.");
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var outboxStore = scope.ServiceProvider.GetRequiredService<IClaimOutboxStore>();
        var nowUtc = timeProvider.GetUtcNow();
        var leaseToken = Guid.NewGuid();
        var messages = await outboxStore.LeaseBatchAsync(
            leaseToken,
            nowUtc,
            LeaseDuration,
            BatchSize,
            cancellationToken);

        if (messages.Count == 0)
        {
            await Task.Delay(PollInterval, cancellationToken);
            return;
        }

        foreach (var message in messages)
        {
            try
            {
                await kafkaProducer.PublishAsync(message, cancellationToken);
            }
            catch (KafkaException exception)
            {
                await RetryMessageAsync(outboxStore, message, leaseToken, exception, cancellationToken);
                continue;
            }
            catch (KafkaUnavailableException exception)
            {
                await RetryMessageAsync(outboxStore, message, leaseToken, exception, cancellationToken);
                continue;
            }

            await outboxStore.MarkPublishedAsync(
                message.MessageId,
                leaseToken,
                timeProvider.GetUtcNow(),
                cancellationToken);
        }
    }

    private async Task RetryMessageAsync(
        IClaimOutboxStore outboxStore,
        LeasedClaimIntegrationEvent message,
        Guid leaseToken,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var delaySeconds = Math.Min(300, Math.Pow(2, Math.Min(message.Attempts, 8)));
        var retryAtUtc = timeProvider.GetUtcNow().AddSeconds(delaySeconds);

        logger.LogWarning(
            exception,
            "Publishing claim event {EventId} failed; retrying at {RetryAtUtc}.",
            message.EventId,
            retryAtUtc);

        await outboxStore.MarkFailedAsync(
            message.MessageId,
            leaseToken,
            retryAtUtc,
            exception.GetType().Name,
            cancellationToken);
    }
}
