using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Messaging.Outbox;

public sealed class InboxMessageConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(
            "InboxMessages",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "CK_InboxMessages_ConsumerName_Valid",
                "length(trim(\"ConsumerName\")) BETWEEN 1 AND 200"));

        builder.HasKey(message => new
        {
            message.MessageId,
            message.ConsumerName
        });

        builder.Property(message => message.ConsumerName)
            .HasMaxLength(InboxMessage.MaximumConsumerNameLength);

        builder.Property(message => message.ProcessedAtUtc)
            .IsRequired();

        builder.HasIndex(message => message.ProcessedAtUtc);
    }
}
