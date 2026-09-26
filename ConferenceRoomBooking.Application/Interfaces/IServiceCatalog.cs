using ConferenceRoomBooking.Application.DTOs.Services;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IServiceCatalog
{
    Task<IReadOnlyList<ServiceResponse>> GetAllAsync(CancellationToken cancellationToken);
}
