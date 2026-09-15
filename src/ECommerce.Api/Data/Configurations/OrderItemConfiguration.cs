using ECommerce.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class OrderItemConfiguration
    : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable(
            "OrderItems",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_OrderItems_ProductName_Valid",
                    "length(trim(\"ProductName\")) BETWEEN 1 AND 200");

                tableBuilder.HasCheckConstraint(
                    "CK_OrderItems_UnitPrice_Positive",
                    "CAST(\"UnitPrice\" AS NUMERIC) > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_OrderItems_Quantity_Positive",
                    "\"Quantity\" > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_OrderItems_LineTotal_Positive",
                    "CAST(\"LineTotal\" AS NUMERIC) > 0");
            });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProductName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2);

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2);

        builder.HasOne(item => item.Order)
            .WithMany(order => order.Items)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(item => item.Product)
            .WithMany(product => product.OrderItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(item => new
        {
            item.OrderId,
            item.ProductId
        }).IsUnique();
    }
}
