using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Chubb.Apac.ClaimsManagement.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.SequenceNumber).UseIdentityColumn();
        builder.Property(message => message.EventType).HasMaxLength(100).IsRequired();
        builder.Property(message => message.ClaimReference).HasMaxLength(40).IsRequired();
        builder.Property(message => message.Payload).IsRequired();
        builder.Property(message => message.OccurredAtUtc).IsRequired();
        builder.Property(message => message.CreatedAtUtc).IsRequired();
        builder.Property(message => message.NextAttemptAtUtc).IsRequired();
        builder.Property(message => message.Attempts).IsRequired();
        builder.Property(message => message.LastError).HasMaxLength(2000);
        builder.Property(message => message.RowVersion).IsRowVersion().IsRequired();

        builder.HasIndex(message => message.EventId).IsUnique();
        builder.HasIndex(message => new
            {
                message.PublishedAtUtc,
                message.NextAttemptAtUtc,
                message.SequenceNumber
            })
            .HasFilter("[PublishedAtUtc] IS NULL");
    }
}
