using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Outcomes;
using ECommerce.Application.Addresses.Services;
using ECommerce.Application.Addresses.Validation;
using ECommerce.Api.Data;
using ECommerce.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace ECommerce.Api.Features.Addresses.Services;

public sealed class EfCoreCustomerAddressService
    : ICustomerAddressService
{
    private readonly ECommerceDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public EfCoreCustomerAddressService(
        ECommerceDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<CustomerAddress>> GetAllAsync(
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerAddresses
            .AsNoTracking()
            .Where(address => address.CustomerId == customerId)
            .OrderByDescending(address => address.IsDefault)
            .ThenByDescending(address => address.UpdatedAtUtc)
            .ThenByDescending(address => address.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<CustomerAddress?> GetByIdAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.CustomerAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                address =>
                    address.Id == id &&
                    address.CustomerId == customerId,
                cancellationToken);
    }

    public async Task<CustomerAddressMutationResult> CreateAsync(
        Guid customerId,
        CreateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var customerExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                customer => customer.Id == customerId,
                cancellationToken);

        if (!customerExists)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new CustomerAddressMutationResult(
                CustomerAddressMutationStatus.CustomerNotFound);
        }

        var addressCount = await _dbContext.CustomerAddresses
            .CountAsync(
                address => address.CustomerId == customerId,
                cancellationToken);

        if (addressCount >=
            CustomerAddressRequestValidator.MaximumAddressesPerCustomer)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new CustomerAddressMutationResult(
                CustomerAddressMutationStatus.AddressLimitReached);
        }

        var makeDefault = request.IsDefault || addressCount == 0;

        if (makeDefault)
        {
            await ClearDefaultAddressAsync(
                customerId,
                exceptAddressId: null,
                cancellationToken);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var address = new CustomerAddress
        {
            CustomerId = customerId,
            IsDefault = makeDefault,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        ApplyValues(
            address,
            request.Label,
            request.RecipientFullName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.District,
            request.City,
            request.PostalCode,
            request.CountryCode);

        _dbContext.CustomerAddresses.Add(address);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CustomerAddressMutationResult(
            CustomerAddressMutationStatus.Success,
            address);
    }

    public async Task<CustomerAddressMutationResult> UpdateAsync(
        int id,
        Guid customerId,
        UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var address = await _dbContext.CustomerAddresses
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.CustomerId == customerId,
                cancellationToken);

        if (address is null)
        {
            await transaction.RollbackAsync(cancellationToken);

            return new CustomerAddressMutationResult(
                CustomerAddressMutationStatus.AddressNotFound);
        }

        var hasDefaultAddress = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .AnyAsync(
                candidate =>
                    candidate.CustomerId == customerId &&
                    candidate.IsDefault,
                cancellationToken);

        var makeDefault =
            request.IsDefault ||
            address.IsDefault ||
            !hasDefaultAddress;

        if (makeDefault)
        {
            await ClearDefaultAddressAsync(
                customerId,
                address.Id,
                cancellationToken);
        }

        ApplyValues(
            address,
            request.Label,
            request.RecipientFullName,
            request.PhoneNumber,
            request.AddressLine1,
            request.AddressLine2,
            request.District,
            request.City,
            request.PostalCode,
            request.CountryCode);

        address.IsDefault = makeDefault;
        address.UpdatedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new CustomerAddressMutationResult(
            CustomerAddressMutationStatus.Success,
            address);
    }

    public async Task<CustomerAddressMutationStatus> DeleteAsync(
        int id,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken);

        var address = await _dbContext.CustomerAddresses
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.Id == id &&
                    candidate.CustomerId == customerId,
                cancellationToken);

        if (address is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CustomerAddressMutationStatus.AddressNotFound;
        }

        var deletedAddresses = await _dbContext.CustomerAddresses
            .Where(candidate =>
                candidate.Id == id &&
                candidate.CustomerId == customerId)
            .ExecuteDeleteAsync(cancellationToken);

        if (deletedAddresses == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return CustomerAddressMutationStatus.AddressNotFound;
        }

        if (address.IsDefault)
        {
            var replacementId = await _dbContext.CustomerAddresses
                .AsNoTracking()
                .Where(candidate => candidate.CustomerId == customerId)
                .OrderByDescending(candidate => candidate.UpdatedAtUtc)
                .ThenByDescending(candidate => candidate.Id)
                .Select(candidate => (int?)candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (replacementId.HasValue)
            {
                await _dbContext.CustomerAddresses
                    .Where(candidate =>
                        candidate.Id == replacementId.Value &&
                        candidate.CustomerId == customerId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(
                            candidate => candidate.IsDefault,
                            true),
                        cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return CustomerAddressMutationStatus.Success;
    }

    private Task<int> ClearDefaultAddressAsync(
        Guid customerId,
        int? exceptAddressId,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.CustomerAddresses
            .Where(address =>
                address.CustomerId == customerId &&
                address.IsDefault);

        if (exceptAddressId.HasValue)
        {
            query = query.Where(address =>
                address.Id != exceptAddressId.Value);
        }

        return query.ExecuteUpdateAsync(
            setters => setters.SetProperty(
                address => address.IsDefault,
                false),
            cancellationToken);
    }

    private static void ApplyValues(
        CustomerAddress address,
        string label,
        string recipientFullName,
        string phoneNumber,
        string addressLine1,
        string? addressLine2,
        string district,
        string city,
        string postalCode,
        string countryCode)
    {
        address.Label = label.Trim();
        address.RecipientFullName = recipientFullName.Trim();
        address.PhoneNumber = phoneNumber.Trim();
        address.AddressLine1 = addressLine1.Trim();
        address.AddressLine2 = string.IsNullOrWhiteSpace(addressLine2)
            ? null
            : addressLine2.Trim();
        address.District = district.Trim();
        address.City = city.Trim();
        address.PostalCode = postalCode.Trim();
        address.CountryCode = countryCode.Trim().ToUpperInvariant();
    }
}
