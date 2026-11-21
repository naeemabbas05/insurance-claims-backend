using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimStatusHistoryConfiguration : IEntityTypeConfiguration<ClaimStatusHistory>
{
    public void Configure(EntityTypeBuilder<ClaimStatusHistory> builder)
    {
        builder.ToTable("ClaimStatusHistory");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.PreviousStatus)
            .HasConversion<string>()
            .HasMaxLength(32);
        builder.Property(history => history.NewStatus)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(history => history.ChangedAtUtc).IsRequired();
        builder.Property(history => history.Reason)
            .HasMaxLength(1000)
            .IsRequired();

        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(history => history.ChangedByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(history => new { history.ClaimId, history.ChangedAtUtc });
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
