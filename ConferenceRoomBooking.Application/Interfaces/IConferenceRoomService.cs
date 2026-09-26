using ConferenceRoomBooking.Application.DTOs.Rooms;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IConferenceRoomService
{
    Task<IReadOnlyList<ConferenceRoomResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<ConferenceRoomResponse?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ConferenceRoomWriteResult> CreateAsync(
        CreateConferenceRoomRequest request, CancellationToken cancellationToken);
    Task<ConferenceRoomWriteResult> UpdateAsync(
        int id, UpdateConferenceRoomRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
}
