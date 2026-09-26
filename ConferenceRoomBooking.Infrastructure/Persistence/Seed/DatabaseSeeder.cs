using System.Data;
using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Seed;

internal sealed class DatabaseSeeder(AppDbContext dbContext)
{
    private static readonly (string Name, int Capacity, decimal BaseHourlyRate)[] InitialRooms =
    [
        ("Зал А", 50, 2000m),
        ("Зал B", 100, 3500m),
        ("Зал C", 30, 1500m)
    ];

    private static readonly (string Name, decimal Price)[] InitialServices =
    [
        ("Проєктор", 500m),
        ("Wi-Fi", 300m),
        ("Звук", 700m)
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var createdRooms = await AddMissingRoomsAsync(cancellationToken);
        var services = await AddMissingServicesAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var room in createdRooms)
        {
            foreach (var service in services)
            {
                dbContext.RoomServices.Add(new RoomService
                {
                    RoomId = room.Id,
                    Room = room,
                    ServiceId = service.Id,
                    Service = service
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<List<ConferenceRoom>> AddMissingRoomsAsync(CancellationToken cancellationToken)
    {
        var createdRooms = new List<ConferenceRoom>(InitialRooms.Length);
        var createdAtUtc = DateTime.UtcNow;

        foreach (var (name, capacity, baseHourlyRate) in InitialRooms)
        {
            var exists = await dbContext.ConferenceRooms.AnyAsync(room => room.Name == name, cancellationToken);
            if (!exists)
            {
                var room = new ConferenceRoom
                {
                    Name = name,
                    Capacity = capacity,
                    BaseHourlyRate = baseHourlyRate,
                    IsActive = true,
                    CreatedAtUtc = createdAtUtc
                };
                dbContext.ConferenceRooms.Add(room);
                createdRooms.Add(room);
            }
        }

        return createdRooms;
    }

    private async Task<List<Service>> AddMissingServicesAsync(CancellationToken cancellationToken)
    {
        var services = new List<Service>(InitialServices.Length);

        foreach (var (name, price) in InitialServices)
        {
            var service = await dbContext.Services
                .SingleOrDefaultAsync(service => service.Name == name, cancellationToken);

            if (service is null)
            {
                service = new Service
                {
                    Name = name,
                    Price = price,
                    IsActive = true
                };
                dbContext.Services.Add(service);
            }

            services.Add(service);
        }

        return services;
    }
}
