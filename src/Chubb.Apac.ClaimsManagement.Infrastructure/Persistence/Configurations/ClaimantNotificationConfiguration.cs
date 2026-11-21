using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimantNotificationConfiguration : IEntityTypeConfiguration<ClaimantNotification>
{
    public void Configure(EntityTypeBuilder<ClaimantNotification> builder)
    {
        builder.ToTable("ClaimantNotifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Category).HasMaxLength(64).IsRequired();
        builder.Property(notification => notification.Title).HasMaxLength(160).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(1000).IsRequired();
        builder.Property(notification => notification.CreatedAtUtc).IsRequired();
        builder.Property(notification => notification.DeduplicationKey).HasMaxLength(160).IsRequired();

        builder.HasOne(notification => notification.Claimant)
            .WithMany()
            .HasForeignKey(notification => notification.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => new { notification.ClaimantId, notification.CreatedAtUtc });
        builder.HasIndex(notification => notification.DeduplicationKey).IsUnique();
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
