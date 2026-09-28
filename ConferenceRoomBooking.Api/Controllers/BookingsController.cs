using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json",
    Description = "An unexpected server error occurred.")]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a booking and calculate its price")]
    [EndpointDescription("Books an active room using local times without a UTC suffix or offset, within one calendar day " +
        "from 06:00 to 23:00. Selected services must be active and available for the room. " +
        "Rental charges are calculated across the applicable pricing intervals, including partial hours; " +
        "each selected service is charged once. The total and service prices are saved as historical snapshots. " +
        "Overlapping requests, including concurrent requests, return 409. Pricing is calculated during creation; " +
        "this operation saves a booking rather than providing a price-only preview.")]
    [ProducesResponseType<BookingResponse>(StatusCodes.Status201Created, "application/json",
        Description = "The booking was created, with rental, services and total prices in UAH.")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
        Description = "The time range, room ID or selected services are invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json",
        Description = "The selected room does not exist or is inactive.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json",
        Description = "The room already has an overlapping booking.")]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, booking);
    }
}
