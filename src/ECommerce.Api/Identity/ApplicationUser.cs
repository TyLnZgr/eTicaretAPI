using ECommerce.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Api.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
