using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Chubb.Apac.ClaimsManagement.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class StaffUserConfiguration : IEntityTypeConfiguration<StaffUser>
{
    public void Configure(EntityTypeBuilder<StaffUser> builder)
    {
        builder.ToTable("StaffUsers");
        builder.HasKey(staff => staff.Id);
        builder.Property(staff => staff.Name)
            .HasConversion(DomainValueConverters.PersonNameConverter)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(staff => staff.Email)
            .HasConversion(DomainValueConverters.EmailAddressConverter)
            .HasMaxLength(254)
            .IsRequired();
        builder.Property(staff => staff.Role)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(staff => staff.CreatedAtUtc).IsRequired();
        builder.HasIndex(staff => staff.Email).IsUnique();
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();

        builder.HasMany(staff => staff.Markets)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "StaffUserMarket",
                market => market
                    .HasOne<MarketDefinition>()
                    .WithMany()
                    .HasForeignKey("MarketCode")
                    .OnDelete(DeleteBehavior.Restrict),
                staff => staff
                    .HasOne<StaffUser>()
                    .WithMany()
                    .HasForeignKey("StaffUserId")
                    .OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("StaffUserMarkets");
                    join.HasKey("StaffUserId", "MarketCode");
                    join.HasData(ModelSeedData.StaffMarketLinks);
                });
        builder.Navigation(staff => staff.Markets)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasData(ModelSeedData.StaffUsers);
    }
}
