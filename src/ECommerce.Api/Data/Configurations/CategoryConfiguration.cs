using ECommerce.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class CategoryConfiguration
    : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable(
            "Categories",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Categories_Name_Valid",
                    "length(trim(\"Name\")) BETWEEN 1 AND 100");

                tableBuilder.HasCheckConstraint(
                    "CK_Categories_IsActive_Valid",
                    "\"IsActive\" IN (0, 1)");
            });

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100);
    }
}