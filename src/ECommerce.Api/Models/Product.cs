namespace ECommerce.Api.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<StockMovement> StockMovements { get; set; }
        = new List<StockMovement>();
    public ICollection<OrderItem> OrderItems { get; set; }
        = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; }
        = new List<CartItem>();
}
