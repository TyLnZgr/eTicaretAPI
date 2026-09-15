using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Messaging.Outbox;

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
                    "NOT (\"ProcessedAtUtc\" IS NOT NULL AND " +
                    "\"DeadLetteredAtUtc\" IS NOT NULL) AND " +
                    "(\"ProcessedAtUtc\" IS NULL OR " +
                    "(\"NextAttemptAtUtc\" IS NULL AND " +
                    "\"LastError\" IS NULL AND " +
                    "\"LockId\" IS NULL)) AND " +
                    "(\"DeadLetteredAtUtc\" IS NULL OR " +
                    "(\"NextAttemptAtUtc\" IS NULL AND " +
                    "\"LockId\" IS NULL AND \"AttemptCount\" > 0))");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_Lease_Consistent",
                    "(\"LockId\" IS NULL AND \"LockedBy\" IS NULL " +
                    "AND \"LockedUntilUtc\" IS NULL) OR " +
                    "(\"LockId\" IS NOT NULL AND \"LockedBy\" IS NOT NULL " +
                    "AND \"LockedUntilUtc\" IS NOT NULL AND " +
                    "\"ProcessedAtUtc\" IS NULL AND " +
                    "\"DeadLetteredAtUtc\" IS NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_OutboxMessages_LockedBy_Valid",
                    "\"LockedBy\" IS NULL OR " +
                    "length(trim(\"LockedBy\")) BETWEEN 1 AND 200");
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

        builder.Property(message => message.LockId)
            .IsConcurrencyToken();

        builder.Property(message => message.LockedBy)
            .HasMaxLength(OutboxMessage.MaximumWorkerNameLength);

        builder.HasIndex(message => message.DeduplicationKey)
            .IsUnique();

        builder.HasIndex(message => message.LockId);

        builder.HasIndex(message => new
        {
            message.ProcessedAtUtc,
            message.DeadLetteredAtUtc,
            message.NextAttemptAtUtc,
            message.LockedUntilUtc,
            message.OccurredAtUtc
        });
    }
}
