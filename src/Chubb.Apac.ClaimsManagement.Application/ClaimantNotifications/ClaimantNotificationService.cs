using FluentValidation;

namespace Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;

public sealed class ClaimantNotificationService(
    IClaimantNotificationRepository repository,
    IValidator<ClaimantNotificationQuery> queryValidator,
    TimeProvider timeProvider) : IClaimantNotificationService
{
    public async Task<ClaimantNotificationPage> GetNotificationsAsync(
        ClaimantNotificationQuery query,
        CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var (items, totalCount) = await repository.GetNotificationsAsync(
            query.ClaimantId,
            query.Email,
            query.EmailVerified,
            query.Page,
            query.PageSize,
            query.UnreadOnly,
            cancellationToken);

        return new ClaimantNotificationPage(
            query.Page,
            query.PageSize,
            totalCount,
            items.Select(ToItem).ToArray());
    }

    public async Task<ClaimantNotificationItem?> MarkReadAsync(
        Guid notificationId,
        Guid? claimantId,
        string? email,
        bool emailVerified,
        CancellationToken cancellationToken)
    {
        if (notificationId == Guid.Empty)
        {
            return null;
        }

        var notification = await repository.FindOwnedNotificationAsync(
            notificationId,
            claimantId,
            email,
            emailVerified,
            cancellationToken);
        if (notification is null)
        {
            return null;
        }

        notification.MarkRead(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return ToItem(notification);
    }

    private static ClaimantNotificationItem ToItem(
        Domain.Entities.ClaimantNotification notification) =>
        new(
            notification.Id,
            notification.Claim.Number.Value,
            notification.Category,
            notification.Title,
            notification.Message,
            notification.CreatedAtUtc,
            notification.ReadAtUtc);
}
