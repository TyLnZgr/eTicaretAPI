namespace ECommerce.Api.Features.Addresses.Outcomes;

public enum CustomerAddressMutationStatus
{
    Success,
    CustomerNotFound,
    AddressNotFound,
    AddressLimitReached
}
