using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.Enums;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class PolicyConfiguration : IEntityTypeConfiguration<Policy>
{
    public void Configure(EntityTypeBuilder<Policy> builder)
    {
        builder.ToTable("Policies");
        builder.HasKey(policy => policy.Id);
        builder.Property(policy => policy.Number)
            .HasConversion(DomainValueConverters.PolicyNumberConverter)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(policy => policy.MarketCode)
            .HasConversion(DomainValueConverters.MarketCodeConverter)
            .HasMaxLength(2)
            .IsRequired();
        builder.Property(policy => policy.Type)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(policy => policy.PolicyHolderName)
            .HasConversion(DomainValueConverters.PersonNameConverter)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(policy => policy.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(policy => policy.CreatedAtUtc).IsRequired();

        builder.HasOne(policy => policy.Market)
            .WithMany()
            .HasForeignKey(policy => policy.MarketCode)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(policy => policy.CoveragePeriod, period =>
        {
            period.Property(value => value.StartsAtUtc)
                .HasColumnName("CoverageStartsAtUtc")
                .IsRequired();
            period.Property(value => value.EndsAtUtc)
                .HasColumnName("CoverageEndsAtUtc")
                .IsRequired();
        });
        builder.Navigation(policy => policy.CoveragePeriod).IsRequired();

        builder.OwnsOne(policy => policy.CoverageLimit, limit =>
        {
            limit.Property(value => value.Amount)
                .HasColumnName("CoverageLimitAmount")
                .HasPrecision(19, 4)
                .IsRequired();
            limit.Property(value => value.Currency)
                .HasColumnName("CoverageLimitCurrency")
                .HasConversion(DomainValueConverters.CurrencyCodeConverter)
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(policy => policy.CoverageLimit).IsRequired();

        builder.HasIndex(policy => policy.Number).IsUnique();
        builder.HasIndex(policy => new { policy.MarketCode, policy.Type, policy.Status });
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();

        builder.HasData(ModelSeedData.Policies);
        builder.OwnsOne(policy => policy.CoveragePeriod)
            .HasData(ModelSeedData.PolicyCoveragePeriods);
        builder.OwnsOne(policy => policy.CoverageLimit)
            .HasData(ModelSeedData.PolicyCoverageLimits);
    }
}
