using Chubb.Apac.ClaimsManagement.Domain.Entities;

namespace Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;

public interface IClaimantNotificationRepository
{
    Task<(IReadOnlyList<ClaimantNotification> Items, int TotalCount)> GetNotificationsAsync(
        Guid? claimantId,
        string? email,
        bool emailVerified,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken);

    Task<ClaimantNotification?> FindOwnedNotificationAsync(
        Guid notificationId,
        Guid? claimantId,
        string? email,
        bool emailVerified,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
