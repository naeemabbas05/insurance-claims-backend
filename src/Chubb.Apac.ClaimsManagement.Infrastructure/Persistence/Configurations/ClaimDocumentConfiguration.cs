using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimDocumentConfiguration : IEntityTypeConfiguration<ClaimDocument>
{
    public void Configure(EntityTypeBuilder<ClaimDocument> builder)
    {
        builder.ToTable("ClaimDocuments");
        builder.HasKey(document => document.Id);
        builder.Property(document => document.Type)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(document => document.UploadedAtUtc).IsRequired();

        builder.HasOne<Claimant>()
            .WithMany()
            .HasForeignKey(document => document.UploadedByClaimantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(document => document.UploadedByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(document => document.Metadata, metadata =>
        {
            metadata.Property(value => value.FileName)
                .HasColumnName("FileName")
                .HasMaxLength(255)
                .IsRequired();
            metadata.Property(value => value.ContentType)
                .HasColumnName("ContentType")
                .HasMaxLength(128)
                .IsRequired();
            metadata.Property(value => value.StorageKey)
                .HasColumnName("StorageKey")
                .HasMaxLength(512)
                .IsRequired();
            metadata.Property(value => value.FileSizeBytes)
                .HasColumnName("FileSizeBytes")
                .IsRequired();
            metadata.Property(value => value.Sha256)
                .HasColumnName("Sha256")
                .HasMaxLength(64);
        });
        builder.Navigation(document => document.Metadata).IsRequired();

        builder.HasIndex(document => document.ClaimId);
        builder.HasIndex(document => document.UploadedByClaimantId);
        builder.HasIndex(document => document.UploadedByStaffUserId);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
