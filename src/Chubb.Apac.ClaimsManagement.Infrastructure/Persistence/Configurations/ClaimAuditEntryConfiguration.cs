using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimAuditEntryConfiguration : IEntityTypeConfiguration<ClaimAuditEntry>
{
    public void Configure(EntityTypeBuilder<ClaimAuditEntry> builder)
    {
        builder.ToTable("ClaimAuditHistory");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Action)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(entry => entry.OccurredAtUtc).IsRequired();
        builder.Property(entry => entry.Details).HasMaxLength(2000);

        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(entry => entry.StaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Claimant>()
            .WithMany()
            .HasForeignKey(entry => entry.ClaimantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(entry => new { entry.ClaimId, entry.OccurredAtUtc });
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
