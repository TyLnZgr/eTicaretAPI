using ECommerce.Domain.Carts;
using ECommerce.Domain.Customers;
using ECommerce.Domain.Notifications;
using ECommerce.Domain.Orders;
using Microsoft.AspNetCore.Identity;

namespace ECommerce.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Cart? Cart { get; set; }
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<CustomerAddress> Addresses { get; set; }
        = new List<CustomerAddress>();
    public ICollection<CustomerNotification> Notifications { get; set; }
        = new List<CustomerNotification>();
}
