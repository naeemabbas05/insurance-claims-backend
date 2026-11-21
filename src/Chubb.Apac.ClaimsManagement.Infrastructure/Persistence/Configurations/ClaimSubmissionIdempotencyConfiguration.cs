using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimSubmissionIdempotencyConfiguration
    : IEntityTypeConfiguration<ClaimSubmissionIdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<ClaimSubmissionIdempotencyRecord> builder)
    {
        builder.ToTable("ClaimSubmissionIdempotency");
        builder.HasKey(record => record.KeyHash);
        builder.Property(record => record.KeyHash)
            .HasColumnType("char(64)")
            .IsFixedLength()
            .IsRequired();
        builder.Property(record => record.RequestFingerprint)
            .HasColumnType("char(64)")
            .IsFixedLength()
            .IsRequired();
        builder.Property(record => record.CreatedAtUtc).IsRequired();

        builder.HasOne(record => record.Claim)
            .WithOne()
            .HasForeignKey<ClaimSubmissionIdempotencyRecord>(record => record.ClaimId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(record => record.ClaimId).IsUnique();
    }
}
