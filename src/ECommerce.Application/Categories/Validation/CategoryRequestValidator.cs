using ECommerce.Domain.Catalog;

namespace ECommerce.Application.Categories.Validation;

public static class CategoryRequestValidator
{
    public static Dictionary<string, string[]> ValidateName(string name)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = new[]
            {
                "Category name is required."
            };
        }
        else if (name.Trim().Length > Category.MaxNameLength)
        {
            errors["name"] = new[]
            {
                $"Category name cannot exceed {Category.MaxNameLength} characters."
            };
        }

        return errors;
    }
}
