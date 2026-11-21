using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class MarketDefinitionConfiguration : IEntityTypeConfiguration<MarketDefinition>
{
    public void Configure(EntityTypeBuilder<MarketDefinition> builder)
    {
        builder.ToTable("Markets");
        builder.HasKey(market => market.Code);
        builder.Property(market => market.Code)
            .HasConversion(DomainValueConverters.MarketCodeConverter)
            .HasMaxLength(2)
            .ValueGeneratedNever();
        builder.Property(market => market.Name)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(market => market.DefaultCurrency)
            .HasConversion(DomainValueConverters.CurrencyCodeConverter)
            .HasMaxLength(3)
            .IsRequired();

        builder.HasData(MarketCatalog.Default.Markets.Cast<object>().ToArray());
    }
}
