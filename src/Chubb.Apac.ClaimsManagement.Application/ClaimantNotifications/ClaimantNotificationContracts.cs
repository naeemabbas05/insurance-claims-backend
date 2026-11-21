namespace Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;

public sealed record ClaimantNotificationQuery(
    Guid? ClaimantId,
    string? Email,
    bool EmailVerified,
    int Page,
    int PageSize,
    bool UnreadOnly);

public sealed record ClaimantNotificationItem(
    Guid Id,
    string ClaimReference,
    string Category,
    string Title,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ReadAtUtc);

public sealed record ClaimantNotificationPage(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<ClaimantNotificationItem> Items);

public interface IClaimantNotificationService
{
    Task<ClaimantNotificationPage> GetNotificationsAsync(
        ClaimantNotificationQuery query,
        CancellationToken cancellationToken);

    Task<ClaimantNotificationItem?> MarkReadAsync(
        Guid notificationId,
        Guid? claimantId,
        string? email,
        bool emailVerified,
        CancellationToken cancellationToken);
}
