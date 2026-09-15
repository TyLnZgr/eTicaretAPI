using ECommerce.Api.Identity;
using ECommerce.Domain.Carts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommerce.Api.Data.Configurations;

public sealed class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(cart => cart.Id);

        builder.Property(cart => cart.CustomerId)
            .IsRequired();

        builder.Property(cart => cart.CreatedAtUtc)
            .IsRequired();

        builder.Property(cart => cart.UpdatedAtUtc)
            .IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithOne(customer => customer.Cart)
            .HasForeignKey<Cart>(cart => cart.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(cart => cart.CustomerId)
            .IsUnique();
    }
}
