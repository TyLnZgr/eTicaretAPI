namespace ECommerce.Api.Features.Categories.Validation;

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
        else if (name.Trim().Length > 100)
        {
            errors["name"] = new[]
            {
                "Category name cannot exceed 100 characters."
            };
        }

        return errors;
    }
}
