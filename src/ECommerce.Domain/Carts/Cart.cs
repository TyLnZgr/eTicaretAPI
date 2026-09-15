namespace ECommerce.Domain.Carts;

public sealed class Cart
{
    public int Id { get; set; }
    public Guid CustomerId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<CartItem> Items { get; set; }
        = new List<CartItem>();
}
