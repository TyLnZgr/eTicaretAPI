using ECommerce.Api.Models;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Api.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerAddress> Addresses { get; set; }
        = new List<CustomerAddress>();
}
