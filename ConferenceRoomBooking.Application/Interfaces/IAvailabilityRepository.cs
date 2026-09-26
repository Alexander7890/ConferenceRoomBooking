using ConferenceRoomBooking.Application.DTOs.Rooms;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IAvailabilityRepository
{
    Task<IReadOnlyList<ConferenceRoomResponse>> FindAsync(
        DateTime startDateTime, DateTime endDateTime, int capacity, CancellationToken cancellationToken);
}
