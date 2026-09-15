namespace ECommerce.Domain.Carts;

public sealed class Cart
{
    private readonly List<CartItem> _items = [];

    internal Cart()
    {
    }

    public Cart(Guid customerId, DateTime createdAtUtc)
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "A customer ID is required.",
                nameof(customerId));
        }

        CustomerId = customerId;
        CreatedAtUtc = NormalizeUtc(createdAtUtc);
        UpdatedAtUtc = CreatedAtUtc;
    }

    public int Id { get; set; }
    public Guid CustomerId { get; internal set; }
    public DateTime CreatedAtUtc { get; internal set; }
    public DateTime UpdatedAtUtc { get; internal set; }

    public IReadOnlyCollection<CartItem> Items => _items;

    public bool SetItemQuantity(
        int productId,
        int quantity,
        DateTime updatedAtUtc)
    {
        var item = _items.SingleOrDefault(candidate =>
            candidate.ProductId == productId);

        if (item is null)
        {
            _items.Add(new CartItem(productId, quantity));
            Touch(updatedAtUtc);
            return true;
        }

        if (!item.ChangeQuantity(quantity))
        {
            return false;
        }

        Touch(updatedAtUtc);
        return true;
    }

    public bool RemoveItem(int productId, DateTime updatedAtUtc)
    {
        if (productId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(productId),
                "A valid product ID is required.");
        }

        var item = _items.SingleOrDefault(candidate =>
            candidate.ProductId == productId);

        if (item is null)
        {
            return false;
        }

        _items.Remove(item);
        Touch(updatedAtUtc);
        return true;
    }

    public bool Clear(DateTime updatedAtUtc)
    {
        if (_items.Count == 0)
        {
            return false;
        }

        _items.Clear();
        Touch(updatedAtUtc);
        return true;
    }

    private void Touch(DateTime updatedAtUtc)
    {
        UpdatedAtUtc = NormalizeUtc(updatedAtUtc);
    }

    private static DateTime NormalizeUtc(DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}
