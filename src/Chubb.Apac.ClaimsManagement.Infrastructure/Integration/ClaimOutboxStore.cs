using System.Data;
using Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Integration;

internal sealed class ClaimOutboxStore(
    ClaimsDbContext dbContext) : IClaimOutboxStore
{
    public async Task<IReadOnlyList<LeasedClaimIntegrationEvent>> LeaseBatchAsync(
        Guid leaseToken,
        DateTimeOffset nowUtc,
        TimeSpan leaseDuration,
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.PublishedAtUtc == null &&
                message.NextAttemptAtUtc <= nowUtc &&
                (message.LeaseExpiresAtUtc == null || message.LeaseExpiresAtUtc <= nowUtc))
            .OrderBy(message => message.SequenceNumber)
            .Take(batchSize)
            .ToArrayAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.Lease(leaseToken, nowUtc.Add(leaseDuration), nowUtc);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return messages.Select(message => new LeasedClaimIntegrationEvent(
            message.Id,
            message.EventId,
            message.EventType,
            message.ClaimReference,
            message.Payload,
            message.Attempts)).ToArray();
    }

    public async Task MarkPublishedAsync(
        Guid messageId,
        Guid leaseToken,
        DateTimeOffset publishedAtUtc,
        CancellationToken cancellationToken)
    {
        var message = await FindLeasedMessageAsync(messageId, leaseToken, cancellationToken);
        message.MarkPublished(leaseToken, publishedAtUtc);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid messageId,
        Guid leaseToken,
        DateTimeOffset nextAttemptAtUtc,
        string error,
        CancellationToken cancellationToken)
    {
        var message = await FindLeasedMessageAsync(messageId, leaseToken, cancellationToken);
        message.MarkFailed(leaseToken, nextAttemptAtUtc, error);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<OutboxMessage> FindLeasedMessageAsync(
        Guid messageId,
        Guid leaseToken,
        CancellationToken cancellationToken)
    {
        var message = await dbContext.OutboxMessages
            .SingleOrDefaultAsync(item => item.Id == messageId, cancellationToken);
        return message is { LeaseToken: var token } && token == leaseToken
            ? message
            : throw new OutboxLeaseLostException(messageId);
    }
}
