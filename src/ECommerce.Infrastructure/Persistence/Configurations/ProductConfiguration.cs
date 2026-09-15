using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable(
            "Products",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Products_Name_Valid",
                    $"length(trim(\"Name\")) BETWEEN 1 AND " +
                    $"{Product.MaxNameLength}");

                tableBuilder.HasCheckConstraint(
                    "CK_Products_Price_Positive",
                    "CAST(\"Price\" AS NUMERIC) > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Products_StockQuantity_NonNegative",
                    "\"StockQuantity\" >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_Products_IsActive_Valid",
                    "\"IsActive\" IN (0, 1)");

                tableBuilder.HasCheckConstraint(
                    "CK_Products_IsDeleted_Valid",
                    "\"IsDeleted\" IN (0, 1)");

                tableBuilder.HasCheckConstraint(
                    "CK_Products_DeletionState_Valid",
                    "(\"IsDeleted\" = 0 AND \"DeletedAtUtc\" IS NULL) OR " +
                    "(\"IsDeleted\" = 1 AND \"DeletedAtUtc\" IS NOT NULL)");
            });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(Product.MaxNameLength);

        builder.Property(product => product.Price)
            .HasPrecision(18, 2);

        builder.Property(product => product.Version)
            .IsRequired()
            .IsConcurrencyToken();

        builder.Property(product => product.CreatedAtUtc)
            .IsRequired();

        builder.Property(product => product.UpdatedAtUtc)
            .IsRequired();

        builder.HasQueryFilter(product => !product.IsDeleted);

        builder.HasOne(product => product.Category)
            .WithMany(category => category.Products)
            .HasForeignKey(product => product.CategoryId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.Navigation(product => product.StockMovements)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.OrderItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(product => product.CartItems)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
