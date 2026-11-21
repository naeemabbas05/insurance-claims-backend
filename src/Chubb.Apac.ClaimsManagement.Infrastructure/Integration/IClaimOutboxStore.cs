namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal interface IClaimOutboxStore
{
    Task<IReadOnlyList<LeasedClaimIntegrationEvent>> LeaseBatchAsync(
        Guid leaseToken,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkPublishedAsync(
        Guid messageId,
        Guid leaseToken,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken);

    Task MarkFailedAsync(
        Guid messageId,
        Guid leaseToken,
        DateTimeOffset nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken);
}

internal sealed record LeasedClaimIntegrationEvent(
    Guid MessageId,
    Guid EventId,
    string EventType,
    string ClaimReference,
    string Payload,
    int Attempts);

internal sealed class OutboxLeaseLostException(Guid messageId)
    : Exception($"The outbox message lease for '{messageId}' is no longer available.");
