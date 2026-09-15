using ECommerce.Domain.Payments;

namespace ECommerce.Domain.Orders;

public class Order
{
    public const int MaxItemCount = 100;
    public const int MaxCustomerEmailLength = 254;
    public const int CurrencyLength = 3;

    private readonly List<OrderItem> _items = [];

    internal Order()
    {
    }

    public Order(
        Guid customerId,
        string customerEmail,
        OrderAddressSnapshot shippingAddress,
        DateTime createdAtUtc,
        string currency = "TRY")
    {
        if (customerId == Guid.Empty)
        {
            throw new ArgumentException(
                "A customer ID is required.",
                nameof(customerId));
        }

        CustomerId = customerId;
        CustomerEmail = NormalizeCustomerEmail(customerEmail);
        Currency = NormalizeCurrency(currency);
        CreatedAtUtc = DateTime.SpecifyKind(
            createdAtUtc,
            DateTimeKind.Utc);

        SetShippingAddress(shippingAddress);
    }

    public int Id { get; set; }
    public Guid? CustomerId { get; internal set; }
    public string CustomerEmail { get; internal set; } = string.Empty;
    public OrderStatus Status { get; private set; } = OrderStatus.Pending;
    public decimal TotalAmount { get; internal set; }
    public string Currency { get; internal set; } = "TRY";
    public DateTime CreatedAtUtc { get; internal set; }
    public OrderAddressSnapshot? ShippingAddress { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items;
    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();

    public OrderItem AddItem(
        int productId,
        string productName,
        decimal unitPrice,
        int quantity)
    {
        if (_items.Count >= MaxItemCount)
        {
            throw new InvalidOperationException(
                $"An order cannot contain more than {MaxItemCount} items.");
        }

        if (_items.Any(item => item.ProductId == productId))
        {
            throw new InvalidOperationException(
                "The same product cannot appear more than once in an order.");
        }

        var item = new OrderItem(
            productId,
            productName,
            unitPrice,
            quantity);

        var newTotalAmount = checked(TotalAmount + item.LineTotal);

        _items.Add(item);
        TotalAmount = newTotalAmount;

        return item;
    }

    public void EnsureReadyForPlacement()
    {
        if (ShippingAddress is null)
        {
            throw new InvalidOperationException(
                "A shipping address is required to place an order.");
        }

        if (_items.Count == 0)
        {
            throw new InvalidOperationException(
                "An order must contain at least one item.");
        }

        if (TotalAmount <= 0)
        {
            throw new InvalidOperationException(
                "The order total must be greater than zero.");
        }
    }

    public void SetShippingAddress(OrderAddressSnapshot shippingAddress)
    {
        ArgumentNullException.ThrowIfNull(shippingAddress);

        if (ShippingAddress is not null)
        {
            throw new InvalidOperationException(
                "The shipping address snapshot has already been assigned.");
        }

        ShippingAddress = shippingAddress;
    }

    public bool CanTransitionTo(OrderStatus nextStatus)
    {
        return IsTransitionAllowed(Status, nextStatus);
    }

    public bool TryTransitionTo(OrderStatus nextStatus)
    {
        if (!CanTransitionTo(nextStatus))
        {
            return false;
        }

        Status = nextStatus;
        return true;
    }

    public static bool IsTransitionAllowed(
        OrderStatus currentStatus,
        OrderStatus nextStatus)
    {
        return (currentStatus, nextStatus) switch
        {
            (OrderStatus.Pending, OrderStatus.PaymentProcessing) => true,
            (OrderStatus.Pending, OrderStatus.Paid) => true,
            (OrderStatus.Pending, OrderStatus.Cancelled) => true,
            (OrderStatus.PaymentProcessing, OrderStatus.Paid) => true,
            (OrderStatus.PaymentProcessing, OrderStatus.Pending) => true,
            (OrderStatus.Paid, OrderStatus.Shipped) => true,
            (OrderStatus.Shipped, OrderStatus.Completed) => true,
            _ => false
        };
    }

    private static string NormalizeCustomerEmail(string customerEmail)
    {
        if (string.IsNullOrWhiteSpace(customerEmail))
        {
            throw new ArgumentException(
                "A customer email is required.",
                nameof(customerEmail));
        }

        var normalizedEmail = customerEmail.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > MaxCustomerEmailLength)
        {
            throw new ArgumentException(
                $"Customer email cannot exceed " +
                $"{MaxCustomerEmailLength} characters.",
                nameof(customerEmail));
        }

        return normalizedEmail;
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException(
                "A currency is required.",
                nameof(currency));
        }

        var normalizedCurrency = currency.Trim().ToUpperInvariant();

        if (normalizedCurrency.Length != CurrencyLength ||
            normalizedCurrency.Any(character => !char.IsLetter(character)))
        {
            throw new ArgumentException(
                $"Currency must be a {CurrencyLength}-letter code.",
                nameof(currency));
        }

        return normalizedCurrency;
    }
}
