using Chubb.Apac.ClaimsManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class ClaimSettlementConfiguration : IEntityTypeConfiguration<ClaimSettlement>
{
    public void Configure(EntityTypeBuilder<ClaimSettlement> builder)
    {
        builder.ToTable("ClaimSettlements");
        builder.HasKey(settlement => settlement.Id);
        builder.Property(settlement => settlement.IsFinal).IsRequired();
        builder.Property(settlement => settlement.Status)
            .HasConversion<string>()
            .HasMaxLength(24)
            .IsRequired();
        builder.Property(settlement => settlement.ProposedAtUtc).IsRequired();

        builder.HasOne(settlement => settlement.Payee)
            .WithMany()
            .HasForeignKey(settlement => settlement.PayeeClaimantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(settlement => settlement.ProposedByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(settlement => settlement.ApprovedByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StaffUser>()
            .WithMany()
            .HasForeignKey(settlement => settlement.PaidByStaffUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(settlement => settlement.Amount, amount =>
        {
            amount.Property(value => value.Amount)
                .HasColumnName("Amount")
                .HasPrecision(19, 4)
                .IsRequired();
            amount.Property(value => value.Currency)
                .HasColumnName("Currency")
                .HasConversion(DomainValueConverters.CurrencyCodeConverter)
                .HasMaxLength(3)
                .IsRequired();
        });
        builder.Navigation(settlement => settlement.Amount).IsRequired();

        builder.HasIndex(settlement => new { settlement.ClaimId, settlement.Status });
        builder.HasIndex(settlement => settlement.PayeeClaimantId);
        builder.Property<byte[]>("RowVersion").IsRowVersion().IsRequired();
    }
}
