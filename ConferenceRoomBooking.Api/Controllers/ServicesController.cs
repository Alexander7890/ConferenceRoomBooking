using ConferenceRoomBooking.Application.DTOs.Services;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/services")]
public sealed class ServicesController(IServiceCatalog serviceCatalog) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ServiceResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await serviceCatalog.GetAllAsync(cancellationToken));
}
