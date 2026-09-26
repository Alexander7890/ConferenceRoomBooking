using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IConferenceRoomRepository
{
    Task<IReadOnlyList<ConferenceRoom>> GetAllAsync(CancellationToken cancellationToken);
    Task<ConferenceRoom?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<ConferenceRoom?> GetForUpdateAsync(int id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Service>> GetServicesByIdsAsync(
        IReadOnlyCollection<int> serviceIds, CancellationToken cancellationToken);
    void Add(ConferenceRoom room);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
