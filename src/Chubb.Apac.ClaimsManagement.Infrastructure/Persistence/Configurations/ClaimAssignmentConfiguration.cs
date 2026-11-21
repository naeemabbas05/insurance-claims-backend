using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimAssignmentConfiguration : IEntityTypeConfiguration<ClaimAssignment>
{
    public void Configure(EntityTypeBuilder<ClaimAssignment> builder)
    {
        builder.ToTable("ClaimAssignments");
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(assignment => assignment.AssignedAtUtc).IsRequired();

        builder.HasOne(assignment => assignment.StaffUser)
            .WithMany()
            .HasForeignKey(assignment => assignment.StaffUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(assignment => new
            {
                assignment.ClaimId,
                assignment.StaffUserId,
                assignment.Role
            })
            .IsUnique()
            .HasFilter("[UnassignedAtUtc] IS NULL");
        builder.HasIndex(assignment => assignment.StaffUserId);
        builder.HasIndex(assignment => new { assignment.StaffUserId, assignment.ClaimId })
            .HasFilter("[UnassignedAtUtc] IS NULL");
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
