using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimConfiguration : IEntityTypeConfiguration<Claim>
{
    public void Configure(EntityTypeBuilder<Claim> builder)
    {
        builder.ToTable("Claims");
        builder.HasKey(claim => claim.Id);
        builder.Property(claim => claim.Number)
            .HasConversion(DomainValueConverters.ClaimNumberConverter)
            .HasMaxLength(40)
            .IsRequired();
        builder.Property(claim => claim.Type)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(claim => claim.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(claim => claim.IncidentAtUtc).IsRequired();
        builder.Property(claim => claim.ReportedAtUtc).IsRequired();
        builder.Property(claim => claim.LossDescription)
            .HasMaxLength(4000)
            .IsRequired();
        builder.Property(claim => claim.CreatedAtUtc).IsRequired();

        builder.OwnsOne(claim => claim.EstimatedLoss, amount =>
        {
            amount.Property(value => value.Amount)
                .HasColumnName("EstimatedLossAmount")
                .HasPrecision(19, 4)
                .IsRequired();
            amount.Property(value => value.Currency)
                .HasColumnName("EstimatedLossCurrency")
                .HasConversion(DomainValueConverters.CurrencyCodeConverter)
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(claim => claim.EstimatedLoss).IsRequired();

        builder.HasOne(claim => claim.Policy)
            .WithMany(policy => policy.Claims)
            .HasForeignKey(claim => claim.PolicyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(claim => claim.Claimants)
            .WithOne()
            .HasForeignKey(claimant => claimant.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.Claimants)
            .HasField("_claimants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.Documents)
            .WithOne()
            .HasForeignKey(document => document.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.Documents)
            .HasField("_documents")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.Assignments)
            .WithOne()
            .HasForeignKey(assignment => assignment.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.Assignments)
            .HasField("_assignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.Assessments)
            .WithOne()
            .HasForeignKey(assessment => assessment.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.Assessments)
            .HasField("_assessments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.StatusHistory)
            .HasField("_statusHistory")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.InformationRequests)
            .WithOne()
            .HasForeignKey(request => request.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.InformationRequests)
            .HasField("_informationRequests")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(claim => claim.Settlements)
            .WithOne()
            .HasForeignKey(settlement => settlement.ClaimId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(claim => claim.Settlements)
            .HasField("_settlements")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(claim => claim.Number).IsUnique();
        builder.HasIndex(claim => new { claim.PolicyId, claim.IncidentAtUtc });
        builder.HasIndex(claim => new { claim.Status, claim.ReportedAtUtc });
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
