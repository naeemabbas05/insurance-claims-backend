namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        EventType = null!;
        ClaimReference = null!;
        Payload = null!;
    }

    public Guid Id { get; private set; }

    public long SequenceNumber { get; private set; }

    public Guid EventId { get; private set; }

    public string EventType { get; private set; }

    public Guid ClaimId { get; private set; }

    public string ClaimReference { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset NextAttemptAtUtc { get; private set; }

    public int Attempts { get; private set; }

    public Guid? LeaseToken { get; private set; }

    public DateTimeOffset? LeaseExpiresAtUtc { get; private set; }

    public DateTimeOffset? PublishedAtUtc { get; private set; }

    public string? LastError { get; private set; }

    public byte[] RowVersion { get; private set; } = null!;

    public static OutboxMessage Create(
        Guid eventId,
        string eventType,
        Guid claimId,
        string claimReference,
        string payload,
        DateTimeOffset occurredAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(claimReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            EventType = eventType,
            ClaimId = claimId,
            ClaimReference = claimReference,
            Payload = payload,
            OccurredAtUtc = occurredAtUtc,
            CreatedAtUtc = occurredAtUtc,
            NextAttemptAtUtc = occurredAtUtc
        };
    }

    public void Lease(Guid leaseToken, DateTimeOffset leaseExpiresAtUtc, DateTimeOffset nowUtc)
    {
        if (PublishedAtUtc is not null ||
            NextAttemptAtUtc > nowUtc ||
            LeaseExpiresAtUtc > nowUtc)
        {
            throw new InvalidOperationException("The outbox message is not available for leasing.");
        }

        LeaseToken = leaseToken;
        LeaseExpiresAtUtc = leaseExpiresAtUtc;
        Attempts = checked(Attempts + 1);
    }

    public void MarkPublished(Guid leaseToken, DateTimeOffset publishedAtUtc)
    {
        EnsureLease(leaseToken);
        PublishedAtUtc = publishedAtUtc;
        LeaseToken = null;
        LeaseExpiresAtUtc = null;
        LastError = null;
    }

    public void MarkFailed(
        Guid leaseToken,
        DateTimeOffset nextAttemptAtUtc,
        string error)
    {
        EnsureLease(leaseToken);
        NextAttemptAtUtc = nextAttemptAtUtc;
        LastError = error.Length <= 2000 ? error : error[..2000];
        LeaseToken = null;
        LeaseExpiresAtUtc = null;
    }

    private void EnsureLease(Guid leaseToken)
    {
        if (LeaseToken != leaseToken)
        {
            throw new InvalidOperationException("The outbox message lease is no longer owned by this publisher.");
        }
    }
}
