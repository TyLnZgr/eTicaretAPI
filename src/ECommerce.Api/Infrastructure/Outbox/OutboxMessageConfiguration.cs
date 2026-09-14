using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Infrastructure.Outbox;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable(
            "OutboxMessages",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_Type_Valid",
                    "length(trim(\"Type\")) BETWEEN 1 AND 200");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_DeduplicationKey_Valid",
                    "length(trim(\"DeduplicationKey\")) " +
                    "BETWEEN 1 AND 200");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_Payload_NotEmpty",
                    "length(trim(\"Payload\")) > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_AttemptCount_NonNegative",
                    "\"AttemptCount\" >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_ProcessedState_Consistent",
                    "\"ProcessedAtUtc\" IS NULL OR " +
                    "(\"NextAttemptAtUtc\" IS NULL AND " +
                    "\"LastError\" IS NULL)");
            });

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Type)
            .IsRequired()
            .HasMaxLength(OutboxMessage.MaximumTypeLength);

        builder.Property(message => message.DeduplicationKey)
            .IsRequired()
            .HasMaxLength(OutboxMessage.MaximumDeduplicationKeyLength);

        builder.Property(message => message.Payload)
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .IsRequired();

        builder.Property(message => message.AttemptCount)
            .HasDefaultValue(0);

        builder.Property(message => message.LastError)
            .HasMaxLength(OutboxMessage.MaximumErrorLength);

        builder.HasIndex(message => message.DeduplicationKey)
            .IsUnique();

        builder.HasIndex(message => new
        {
            message.ProcessedAtUtc,
            message.NextAttemptAtUtc,
            message.OccurredAtUtc
        });
    }
}
