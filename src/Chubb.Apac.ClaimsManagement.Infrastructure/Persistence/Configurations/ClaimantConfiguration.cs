using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimantConfiguration : IEntityTypeConfiguration<Claimant>
{
    public void Configure(EntityTypeBuilder<Claimant> builder)
    {
        builder.ToTable("Claimants");
        builder.HasKey(claimant => claimant.Id);
        builder.Property(claimant => claimant.ClaimId).IsRequired();
        builder.Property(claimant => claimant.CreatedAtUtc).IsRequired();

        builder.OwnsOne(claimant => claimant.Details, details =>
        {
            details.Property(value => value.Role)
                .HasColumnName("Role")
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            details.OwnsOne(value => value.Name, name =>
            {
                name.Property(value => value.FullName)
                    .HasColumnName("FullName")
                    .HasMaxLength(200)
                    .IsRequired();
            });
            details.Navigation(value => value.Name).IsRequired();

            details.OwnsOne(value => value.Contact, contact =>
            {
                contact.Property(value => value.Email)
                    .HasColumnName("Email")
                    .HasConversion(DomainValueConverters.NullableEmailAddressConverter)
                    .HasMaxLength(254)
                    .IsRequired(false);
                contact.Property(value => value.Phone)
                    .HasColumnName("Phone")
                    .HasConversion(DomainValueConverters.NullablePhoneNumberConverter)
                    .HasMaxLength(30)
                    .IsRequired(false);
            });
            details.Navigation(value => value.Contact).IsRequired();
        });
        builder.Navigation(claimant => claimant.Details).IsRequired();

        builder.HasIndex(claimant => claimant.ClaimId);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
