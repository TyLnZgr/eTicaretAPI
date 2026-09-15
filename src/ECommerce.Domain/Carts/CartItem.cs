using ECommerce.Domain.Catalog;

namespace ECommerce.Domain.Carts;

public sealed class CartItem
{
    public const int MaximumQuantity = 1_000;

    internal CartItem()
    {
    }

    internal CartItem(int productId, int quantity)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "A valid product ID is required.");
        }

        ProductId = productId;
        SetQuantity(quantity);
    }

    public int Id { get; set; }
    public int CartId { get; internal set; }
    public int ProductId { get; internal set; }
    public int Quantity { get; private set; }

    public Cart Cart { get; internal set; } = null!;
    public Product Product { get; internal set; } = null!;

    internal bool ChangeQuantity(int quantity)
    {
        ValidateQuantity(quantity);

        if (Quantity == quantity)
        {
            return false;
        }

        Quantity = quantity;
        return true;
    }

    private void SetQuantity(int quantity)
    {
        ValidateQuantity(quantity);
        Quantity = quantity;
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity < 1 || quantity > MaximumQuantity)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                $"Quantity must be between 1 and {MaximumQuantity}.");
        }
    }
}
