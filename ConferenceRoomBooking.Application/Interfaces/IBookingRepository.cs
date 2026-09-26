using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IBookingRepository
{
    Task<Booking> CreateAsync(
        int roomId,
        DateTime startDateTime,
        DateTime endDateTime,
        Func<ConferenceRoom?, bool, Booking> createBooking,
        CancellationToken cancellationToken);
}
