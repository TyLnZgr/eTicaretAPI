using ECommerce.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration
    : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable(
            "StockMovements",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_StockMovements_QuantityDelta_NonZero",
                    "\"QuantityDelta\" <> 0");

                tableBuilder.HasCheckConstraint(
                    "CK_StockMovements_StockQuantityAfter_NonNegative",
                    "\"StockQuantityAfter\" >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_StockMovements_Reason_Valid",
                    "length(trim(\"Reason\")) BETWEEN 1 AND 200");
            });

        builder.HasKey(movement => movement.Id);

        builder.Property(movement => movement.Reason)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(movement => movement.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(movement => new
        {
            movement.ProductId,
            movement.CreatedAtUtc
        });

        builder.HasOne(movement => movement.Product)
            .WithMany(product => product.StockMovements)
            .HasForeignKey(movement => movement.ProductId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
