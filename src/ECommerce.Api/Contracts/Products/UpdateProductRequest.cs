namespace ECommerce.Api.Contracts.Products;

public class UpdateProductRequest
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;
}
