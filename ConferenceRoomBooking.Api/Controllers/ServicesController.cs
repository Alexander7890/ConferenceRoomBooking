using ConferenceRoomBooking.Application.DTOs.Services;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json",
    Description = "An unexpected server error occurred.")]
public sealed class ServicesController(IServiceCatalog serviceCatalog) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List active additional services")]
    [EndpointDescription("Returns the read-only catalog of active services and their current one-time booking prices in UAH. " +
        "A service must also be assigned to the selected room before it can be booked.")]
    [ProducesResponseType<IReadOnlyList<ServiceResponse>>(StatusCodes.Status200OK, "application/json",
        Description = "The active services, or an empty list.")]
    public async Task<ActionResult<IReadOnlyList<ServiceResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await serviceCatalog.GetAllAsync(cancellationToken));
}
