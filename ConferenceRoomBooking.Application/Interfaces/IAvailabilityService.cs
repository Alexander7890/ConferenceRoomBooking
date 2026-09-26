using ConferenceRoomBooking.Application.DTOs.Rooms;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IAvailabilityService
{
    Task<IReadOnlyList<ConferenceRoomResponse>> FindAsync(
        RoomAvailabilityRequest request, CancellationToken cancellationToken);
}
