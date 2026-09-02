using ECommerce.Api.Models;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var products = new List<Product>
{
    new Product
    {
        Id = 1,
        Name = "Mechanical Keyboard",
        Price = 2499.90m,
        StockQuantity = 25,
        IsActive = true
    },
    new Product
    {
        Id = 2,
        Name = "Wireless Mouse",
        Price = 1299.50m,
        StockQuantity = 40,
        IsActive = true
    },
    new Product
    {
        Id = 3,
        Name = "4K Monitor",
        Price = 12999.00m,
        StockQuantity = 0,
        IsActive = false
    }
};

app.MapGet("/", () => new
{
    message = "ECommerce API is running."
});

app.MapGet("/api/products", () => products);
app.MapGet("/api/products/1", () => products[0]);

app.Run();
