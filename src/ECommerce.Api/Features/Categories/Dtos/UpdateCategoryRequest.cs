namespace ECommerce.Api.Features.Categories.Dtos;

public class UpdateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
