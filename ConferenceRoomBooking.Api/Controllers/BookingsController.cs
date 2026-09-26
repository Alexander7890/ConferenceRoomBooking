using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/bookings")]
public sealed class BookingsController(IBookingService bookingService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<BookingResponse>> Create(
        CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var booking = await bookingService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, booking);
    }
}
