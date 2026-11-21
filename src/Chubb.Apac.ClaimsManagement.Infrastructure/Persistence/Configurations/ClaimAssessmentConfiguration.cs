using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimAssessmentConfiguration : IEntityTypeConfiguration<ClaimAssessment>
{
    public void Configure(EntityTypeBuilder<ClaimAssessment> builder)
    {
        builder.ToTable("ClaimAssessments");
        builder.HasKey(assessment => assessment.Id);
        builder.Property(assessment => assessment.Findings)
            .HasMaxLength(4000)
            .IsRequired();
        builder.Property(assessment => assessment.AssessedAtUtc).IsRequired();

        builder.HasOne(assessment => assessment.Assessor)
            .WithMany()
            .HasForeignKey(assessment => assessment.AssessorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(assessment => assessment.AssessedDamage, amount =>
        {
            amount.Property(value => value.Amount)
                .HasColumnName("AssessedDamageAmount")
                .HasPrecision(19, 4)
                .IsRequired();
            amount.Property(value => value.Currency)
                .HasColumnName("AssessedDamageCurrency")
                .HasConversion(DomainValueConverters.CurrencyCodeConverter)
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(assessment => assessment.AssessedDamage).IsRequired();

        builder.OwnsOne(assessment => assessment.RecommendedSettlement, amount =>
        {
            amount.Property(value => value.Amount)
                .HasColumnName("RecommendedSettlementAmount")
                .HasPrecision(19, 4)
                .IsRequired();
            amount.Property(value => value.Currency)
                .HasColumnName("RecommendedSettlementCurrency")
                .HasConversion(DomainValueConverters.CurrencyCodeConverter)
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(assessment => assessment.RecommendedSettlement).IsRequired();

        builder.HasIndex(assessment => new { assessment.ClaimId, assessment.AssessedAtUtc });
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
