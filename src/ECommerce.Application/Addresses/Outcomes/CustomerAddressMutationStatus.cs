namespace ECommerce.Application.Addresses.Outcomes;

public enum CustomerAddressMutationStatus
{
    Success,
    CustomerNotFound,
    AddressNotFound,
    AddressLimitReached
}
