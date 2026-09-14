using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class CustomerNotificationConfiguration
    : IEntityTypeConfiguration<CustomerNotification>
{
    public void Configure(EntityTypeBuilder<CustomerNotification> builder)
    {
        builder.ToTable(
            "CustomerNotifications",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_Type_Valid",
                    "\"Type\" IN (1)");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_Title_Valid",
                    "length(trim(\"Title\")) BETWEEN 1 AND 200");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_Message_Valid",
                    "length(trim(\"Message\")) BETWEEN 1 AND 1000");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_IsRead_Valid",
                    "\"IsRead\" IN (0, 1)");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_ReadState_Consistent",
                    "(\"IsRead\" = 0 AND \"ReadAtUtc\" IS NULL) OR " +
                    "(\"IsRead\" = 1 AND \"ReadAtUtc\" IS NOT NULL)");
            });

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Type)
            .HasConversion<int>();

        builder.Property(notification => notification.Title)
            .IsRequired()
            .HasMaxLength(CustomerNotification.MaximumTitleLength);

        builder.Property(notification => notification.Message)
            .IsRequired()
            .HasMaxLength(CustomerNotification.MaximumMessageLength);

        builder.Property(notification => notification.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(notification => notification.Customer)
            .WithMany(customer => customer.Notifications)
            .HasForeignKey(notification => notification.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(notification => notification.Order)
            .WithMany()
            .HasForeignKey(notification => notification.OrderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(notification => notification.SourceMessageId)
            .IsUnique();

        builder.HasIndex(notification => new
        {
            notification.CustomerId,
            notification.IsRead,
            notification.CreatedAtUtc
        });
    }
}
