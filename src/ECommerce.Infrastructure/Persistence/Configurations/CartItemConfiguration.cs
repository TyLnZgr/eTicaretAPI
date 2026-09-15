using ECommerce.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Infrastructure.Persistence.Configurations;

public sealed class CartItemConfiguration
    : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable(
            "CartItems",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_CartItems_Quantity_Valid",
                    $"\"Quantity\" BETWEEN 1 AND {CartItem.MaximumQuantity}");
            });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.HasQueryFilter(item => !item.Product.IsDeleted);

        builder.HasOne(item => item.Cart)
            .WithMany(cart => cart.Items)
            .HasForeignKey(item => item.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany(product => product.CartItems)
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => new
        {
            item.CartId,
            item.ProductId
        }).IsUnique();
    }
}
