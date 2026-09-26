using ConferenceRoomBooking.Application.DTOs.Bookings;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IBookingService
{
    Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken);
}
