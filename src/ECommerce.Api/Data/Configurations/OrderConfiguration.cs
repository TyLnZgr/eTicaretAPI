using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable(
            "Orders",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Orders_CustomerEmail_Valid",
                    "length(trim(\"CustomerEmail\")) BETWEEN 3 AND 254");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_Status_Valid",
                    "\"Status\" IN (1, 2, 3, 4, 5)");

                tableBuilder.HasCheckConstraint(
                    "CK_Orders_TotalAmount_Positive",
                    "CAST(\"TotalAmount\" AS NUMERIC) > 0");
            });

        builder.HasKey(order => order.Id);

        builder.Property(order => order.CustomerEmail)
            .IsRequired()
            .HasMaxLength(254);

        builder.Property(order => order.Status)
            .HasConversion<int>();

        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2);

        builder.Property(order => order.CreatedAtUtc)
            .IsRequired();

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(order => new
        {
            order.CustomerId,
            order.CreatedAtUtc
        });
    }
}
