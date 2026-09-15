using ECommerce.Api.Common.Authentication;
using ECommerce.Application.Addresses.Dtos;
using ECommerce.Application.Addresses.Mappings;
using ECommerce.Application.Addresses.Outcomes;
using ECommerce.Application.Addresses.Services;
using ECommerce.Application.Addresses.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ECommerce.Api.Features.Addresses.Controllers;

[ApiController]
[Authorize]
[Route("api/addresses")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public sealed class CustomerAddressesController : ControllerBase
{
    private readonly ICustomerAddressService _addressService;

    public CustomerAddressesController(
        ICustomerAddressService addressService)
    {
        _addressService = addressService;
    }

    [HttpGet(Name = "GetCustomerAddresses")]
    [EndpointSummary("List the current customer's addresses")]
    [ProducesResponseType<CustomerAddressResponse[]>(
        StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerAddressResponse[]>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var addresses = await _addressService.GetAllAsync(
            customerId,
            cancellationToken);

        return Ok(addresses
            .Select(address => address.ToResponse())
            .ToArray());
    }

    [HttpGet("{id:int}", Name = "GetCustomerAddressById")]
    [EndpointSummary("Get one of the current customer's addresses")]
    [ProducesResponseType<CustomerAddressResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<ActionResult<CustomerAddressResponse>> GetByIdAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var address = await _addressService.GetByIdAsync(
            id,
            customerId,
            cancellationToken);

        if (address is null)
        {
            return AddressNotFound(id);
        }

        return Ok(address.ToResponse());
    }

    [HttpPost(Name = "CreateCustomerAddress")]
    [EndpointSummary("Create an address for the current customer")]
    [ProducesResponseType<CustomerAddressResponse>(
        StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status409Conflict,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    public async Task<ActionResult<CustomerAddressResponse>> CreateAsync(
        [FromBody] CreateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = CustomerAddressRequestValidator.Validate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _addressService.CreateAsync(
            customerId,
            request,
            cancellationToken);

        if (result.Status ==
            CustomerAddressMutationStatus.CustomerNotFound)
        {
            return Unauthorized();
        }

        if (result.Status ==
            CustomerAddressMutationStatus.AddressLimitReached)
        {
            return Problem(
                detail: $"A customer can save at most {CustomerAddressRequestValidator.MaximumAddressesPerCustomer} addresses.",
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict");
        }

        if (result.Address is null)
        {
            return InternalServerError();
        }

        return CreatedAtRoute(
            "GetCustomerAddressById",
            new { id = result.Address.Id },
            result.Address.ToResponse());
    }

    [HttpPut("{id:int}", Name = "UpdateCustomerAddress")]
    [EndpointSummary("Update one of the current customer's addresses")]
    [ProducesResponseType<CustomerAddressResponse>(
        StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(
        StatusCodes.Status400BadRequest,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status500InternalServerError,
        "application/problem+json")]
    public async Task<ActionResult<CustomerAddressResponse>> UpdateAsync(
        [FromRoute] int id,
        [FromBody] UpdateCustomerAddressRequest request,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var errors = CustomerAddressRequestValidator.Validate(request);

        if (errors.Count > 0)
        {
            return ValidationProblem(
                new ValidationProblemDetails(errors));
        }

        var result = await _addressService.UpdateAsync(
            id,
            customerId,
            request,
            cancellationToken);

        if (result.Status ==
            CustomerAddressMutationStatus.AddressNotFound)
        {
            return AddressNotFound(id);
        }

        if (result.Address is null)
        {
            return InternalServerError();
        }

        return Ok(result.Address.ToResponse());
    }

    [HttpDelete("{id:int}", Name = "DeleteCustomerAddress")]
    [EndpointSummary("Delete one of the current customer's addresses")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(
        StatusCodes.Status404NotFound,
        "application/problem+json")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        if (!User.TryGetUserId(out var customerId))
        {
            return Unauthorized();
        }

        var status = await _addressService.DeleteAsync(
            id,
            customerId,
            cancellationToken);

        if (status == CustomerAddressMutationStatus.AddressNotFound)
        {
            return AddressNotFound(id);
        }

        return NoContent();
    }

    private ObjectResult AddressNotFound(int id)
    {
        return Problem(
            detail: $"Address with ID {id} was not found.",
            statusCode: StatusCodes.Status404NotFound,
            title: "Not Found");
    }

    private ObjectResult InternalServerError()
    {
        return Problem(
            detail: "The server could not complete the request.",
            statusCode: StatusCodes.Status500InternalServerError,
            title: "Internal Server Error");
    }
}
