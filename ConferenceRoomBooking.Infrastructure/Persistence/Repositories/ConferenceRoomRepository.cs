using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Repositories;

public sealed class ConferenceRoomRepository(AppDbContext dbContext) : IConferenceRoomRepository
{
    public async Task<IReadOnlyList<ConferenceRoom>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await dbContext.ConferenceRooms
            .AsNoTracking()
            .Where(room => room.IsActive)
            .Include(room => room.RoomServices.Where(roomService => roomService.Service.IsActive))
            .ThenInclude(roomService => roomService.Service)
            .OrderBy(room => room.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<ConferenceRoom?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.ConferenceRooms
            .AsNoTracking()
            .Include(room => room.RoomServices.Where(roomService => roomService.Service.IsActive))
            .ThenInclude(roomService => roomService.Service)
            .SingleOrDefaultAsync(room => room.Id == id && room.IsActive, cancellationToken);
    }

    public Task<ConferenceRoom?> GetForUpdateAsync(int id, CancellationToken cancellationToken)
    {
        return dbContext.ConferenceRooms
            .Include(room => room.RoomServices)
            .ThenInclude(roomService => roomService.Service)
            .SingleOrDefaultAsync(room => room.Id == id && room.IsActive, cancellationToken);
    }

    public async Task<IReadOnlyList<Service>> GetServicesByIdsAsync(
        IReadOnlyCollection<int> serviceIds,
        CancellationToken cancellationToken)
    {
        if (serviceIds.Count == 0)
        {
            return [];
        }

        return await dbContext.Services
            .Where(service => serviceIds.Contains(service.Id))
            .ToListAsync(cancellationToken);
    }

    public void Add(ConferenceRoom room)
    {
        dbContext.ConferenceRooms.Add(room);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
