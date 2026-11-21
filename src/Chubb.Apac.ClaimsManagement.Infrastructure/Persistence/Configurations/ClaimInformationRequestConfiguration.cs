using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimInformationRequestConfiguration : IEntityTypeConfiguration<ClaimInformationRequest>
{
    public void Configure(EntityTypeBuilder<ClaimInformationRequest> builder)
    {
        builder.ToTable("ClaimInformationRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Question)
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(request => request.RequestedAtUtc).IsRequired();
        builder.Property(request => request.DueAtUtc).IsRequired();
        builder.Property(request => request.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(request => request.Response).HasMaxLength(4000);

        builder.HasOne(request => request.RequestedFrom)
            .WithMany()
            .HasForeignKey(request => request.RequestedFromClaimantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(request => request.RequestedBy)
            .WithMany()
            .HasForeignKey(request => request.RequestedByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(request => new { request.ClaimId, request.Status, request.DueAtUtc });
        builder.HasIndex(request => request.RequestedFromClaimantId);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
