using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

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
             "length(trim(\"Name\")) BETWEEN 1 AND 200");

         tableBuilder.HasCheckConstraint(
             "CK_Products_Price_Positive",
             "CAST(\"Price\" AS NUMERIC) > 0");

         tableBuilder.HasCheckConstraint(
             "CK_Products_StockQuantity_NonNegative",
             "\"StockQuantity\" >= 0");

         tableBuilder.HasCheckConstraint(
             "CK_Products_IsActive_Valid",
             "\"IsActive\" IN (0, 1)");
     });

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(product => product.Price)
            .HasPrecision(18, 2);

        builder.HasOne(product => product.Category)
     .WithMany(category => category.Products)
     .HasForeignKey(product => product.CategoryId)
     .OnDelete(DeleteBehavior.Restrict)
     .IsRequired();
    }
}