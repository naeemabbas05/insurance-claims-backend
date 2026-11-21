using Chubb.Apac.ClaimsManagement.Application.ClaimantNotifications;
using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence;

public sealed class ClaimantNotificationRepository(ClaimsDbContext dbContext)
    : IClaimantNotificationRepository
{
    public async Task<(IReadOnlyList<ClaimantNotification> Items, int TotalCount)> GetNotificationsAsync(
        Guid? claimantId,
        string? email,
        bool emailVerified,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken cancellationToken)
    {
        var query = FilterOwnedNotifications(
            dbContext.ClaimantNotifications.AsNoTracking(),
            claimantId,
            email,
            emailVerified);
        if (unreadOnly)
        {
            query = query.Where(notification => notification.ReadAtUtc == null);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Include(notification => notification.Claim)
            .OrderByDescending(notification => notification.CreatedAtUtc)
            .ThenByDescending(notification => notification.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToArrayAsync(cancellationToken);
        return (items, totalCount);
    }

    public Task<ClaimantNotification?> FindOwnedNotificationAsync(
        Guid notificationId,
        Guid? claimantId,
        string? email,
        bool emailVerified,
        CancellationToken cancellationToken) =>
        FilterOwnedNotifications(
                dbContext.ClaimantNotifications,
                claimantId,
                email,
                emailVerified)
            .Include(notification => notification.Claim)
            .SingleOrDefaultAsync(
                notification => notification.Id == notificationId,
                cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static IQueryable<ClaimantNotification> FilterOwnedNotifications(
        IQueryable<ClaimantNotification> query,
        Guid? claimantId,
        string? email,
        bool emailVerified)
    {
        var normalizedEmail = emailVerified && !string.IsNullOrWhiteSpace(email)
            ? EmailAddress.Create(email)
            : null;

        return query.Where(notification =>
            (claimantId.HasValue && notification.ClaimantId == claimantId.Value) ||
            (normalizedEmail != null &&
             notification.Claimant.Details.Contact.Email != null &&
             notification.Claimant.Details.Contact.Email == normalizedEmail));
    }
}
