using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Repositories;

public sealed class AvailabilityRepository(AppDbContext dbContext) : IAvailabilityRepository
{
    public async Task<IReadOnlyList<ConferenceRoomResponse>> FindAsync(
        DateTime startDateTime, DateTime endDateTime, int capacity, CancellationToken cancellationToken)
    {
        return await dbContext.ConferenceRooms
            .AsNoTracking()
            .Where(room => room.IsActive && room.Capacity >= capacity)
            .Where(room => !room.Bookings.Any(booking =>
                startDateTime < booking.EndDateTime && endDateTime > booking.StartDateTime))
            .OrderBy(room => room.Id)
            .Select(room => new ConferenceRoomResponse(
                room.Id,
                room.Name,
                room.Capacity,
                room.BaseHourlyRate,
                room.IsActive,
                room.RoomServices
                    .Where(relation => relation.Service.IsActive)
                    .OrderBy(relation => relation.ServiceId)
                    .Select(relation => new ConferenceRoomServiceResponse(
                        relation.Service.Id, relation.Service.Name, relation.Service.Price))
                    .ToArray()))
            .ToListAsync(cancellationToken);
    }
}
