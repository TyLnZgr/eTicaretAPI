namespace ECommerce.Domain.Catalog;

public class Category
{
    public const int MaxNameLength = 100;

    private readonly List<Product> _products = [];

    internal Category()
    {
    }

    public Category(string name, bool isActive)
    {
        UpdateDetails(name, isActive);
    }

    public int Id { get; set; }
    public string Name { get; internal set; } = string.Empty;
    public bool IsActive { get; internal set; }

    public IReadOnlyCollection<Product> Products => _products;

    public void UpdateDetails(string name, bool isActive)
    {
        Name = NormalizeName(name);
        IsActive = isActive;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Category name is required.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Category name cannot exceed {MaxNameLength} characters.",
                nameof(name));
        }

        return normalizedName;
    }
}
