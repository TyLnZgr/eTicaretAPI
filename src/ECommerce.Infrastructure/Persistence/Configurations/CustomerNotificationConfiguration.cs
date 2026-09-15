using ECommerce.Infrastructure.Identity;
using ECommerce.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

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
                    $"\"Type\" IN ({(int)NotificationType.OrderPaid})");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_Title_Valid",
                    $"length(trim(\"Title\")) BETWEEN 1 AND " +
                    $"{CustomerNotification.MaximumTitleLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_CustomerNotifications_Message_Valid",
                    $"length(trim(\"Message\")) BETWEEN 1 AND " +
                    $"{CustomerNotification.MaximumMessageLength}");

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

        builder.HasOne<ApplicationUser>()
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
