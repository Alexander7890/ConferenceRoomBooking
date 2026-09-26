using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Services;
using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Tests.Rooms;

public sealed class ConferenceRoomServiceTests
{
    [Fact]
    public async Task CreateAsync_InvalidRoomDetails_ReturnsErrorsWithoutSaving()
    {
        var repository = new RoomRepository();
        var service = new ConferenceRoomService(repository);

        var result = await service.CreateAsync(new CreateConferenceRoomRequest
        {
            Name = "   ",
            Capacity = 0,
            BaseHourlyRate = -1
        }, CancellationToken.None);

        Assert.Null(result.Room);
        Assert.False(result.IsNotFound);
        Assert.Contains(nameof(CreateConferenceRoomRequest.Name), result.Errors.Keys);
        Assert.Contains(nameof(CreateConferenceRoomRequest.Capacity), result.Errors.Keys);
        Assert.Contains(nameof(CreateConferenceRoomRequest.BaseHourlyRate), result.Errors.Keys);
        Assert.Empty(repository.Rooms);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_DuplicateServiceIds_ReturnsValidationError()
    {
        var repository = new RoomRepository();
        repository.Services.Add(NewService(7));
        var service = new ConferenceRoomService(repository);

        var result = await service.CreateAsync(new CreateConferenceRoomRequest
        {
            Name = "Meeting room",
            Capacity = 20,
            BaseHourlyRate = 1200,
            ServiceIds = [7, 7]
        }, CancellationToken.None);

        Assert.Contains(nameof(CreateConferenceRoomRequest.ServiceIds), result.Errors.Keys);
        Assert.Empty(repository.Rooms);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsync_UnavailableService_DoesNotCreateRoom(bool serviceExists)
    {
        var repository = new RoomRepository();
        if (serviceExists)
        {
            repository.Services.Add(NewService(7, isActive: false));
        }
        var service = new ConferenceRoomService(repository);

        var result = await service.CreateAsync(new CreateConferenceRoomRequest
        {
            Name = "Meeting room",
            Capacity = 20,
            BaseHourlyRate = 1200,
            ServiceIds = [7]
        }, CancellationToken.None);

        Assert.Contains(nameof(CreateConferenceRoomRequest.ServiceIds), result.Errors.Keys);
        Assert.Empty(repository.Rooms);
        Assert.Equal(0, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsync_ValidRequest_SavesActiveRoomWithSelectedServices(bool selectService)
    {
        var repository = new RoomRepository();
        var availableService = NewService(7);
        repository.Services.Add(availableService);
        var service = new ConferenceRoomService(repository);
        using var cancellation = new CancellationTokenSource();
        var beforeCreation = DateTime.UtcNow;

        var result = await service.CreateAsync(new CreateConferenceRoomRequest
        {
            Name = " Meeting room ",
            Capacity = 20,
            BaseHourlyRate = 1200.50m,
            ServiceIds = selectService ? [7] : []
        }, cancellation.Token);

        var room = Assert.Single(repository.Rooms);
        Assert.NotNull(result.Room);
        Assert.Empty(result.Errors);
        Assert.Equal(room.Id, result.Room.Id);
        Assert.True(room.Id > 0);
        Assert.Equal("Meeting room", room.Name);
        Assert.Equal(20, room.Capacity);
        Assert.Equal(1200.50m, room.BaseHourlyRate);
        Assert.True(room.IsActive);
        Assert.InRange(room.CreatedAtUtc, beforeCreation, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, room.CreatedAtUtc.Kind);
        Assert.Null(room.UpdatedAtUtc);
        if (selectService)
        {
            Assert.Same(availableService, Assert.Single(room.RoomServices).Service);
            Assert.Equal(availableService.Id, Assert.Single(result.Room.Services).Id);
        }
        else
        {
            Assert.Empty(room.RoomServices);
            Assert.Empty(result.Room.Services);
        }
        Assert.Equal(1, repository.SaveCount);
        Assert.All(repository.Tokens, token => Assert.Equal(cancellation.Token, token));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAsync_UnavailableService_LeavesTrackedRoomUnchanged(bool serviceExists)
    {
        var repository = new RoomRepository();
        var room = NewRoom();
        var originalRelation = AddRelation(room, NewService(3));
        repository.Rooms.Add(room);
        if (serviceExists)
        {
            repository.Services.Add(NewService(7, isActive: false));
        }
        var service = new ConferenceRoomService(repository);

        var result = await service.UpdateAsync(room.Id, new UpdateConferenceRoomRequest
        {
            Name = "Changed room",
            Capacity = 99,
            BaseHourlyRate = 9000,
            ServiceIds = [7]
        }, CancellationToken.None);

        Assert.Contains(nameof(UpdateConferenceRoomRequest.ServiceIds), result.Errors.Keys);
        Assert.Equal("Original room", room.Name);
        Assert.Equal(10, room.Capacity);
        Assert.Equal(1000m, room.BaseHourlyRate);
        Assert.Null(room.UpdatedAtUtc);
        Assert.Same(originalRelation, Assert.Single(room.RoomServices));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_ChangedServices_ReconcilesRelationsInOneSave()
    {
        var repository = new RoomRepository();
        var room = NewRoom();
        var createdAt = room.CreatedAtUtc;
        var removedService = NewService(3);
        var retainedService = NewService(7);
        var addedService = NewService(11);
        var removedRelation = AddRelation(room, removedService);
        var retainedRelation = AddRelation(room, retainedService);
        repository.Rooms.Add(room);
        repository.Services.AddRange([removedService, retainedService, addedService]);
        var service = new ConferenceRoomService(repository);
        var beforeUpdate = DateTime.UtcNow;

        var result = await service.UpdateAsync(room.Id, new UpdateConferenceRoomRequest
        {
            Name = " Updated room ",
            Capacity = 30,
            BaseHourlyRate = 2500,
            ServiceIds = [7, 11]
        }, CancellationToken.None);

        Assert.Empty(result.Errors);
        Assert.NotNull(result.Room);
        Assert.Same(room, Assert.Single(repository.Rooms));
        Assert.Equal("Updated room", room.Name);
        Assert.Equal(30, room.Capacity);
        Assert.Equal(2500m, room.BaseHourlyRate);
        Assert.Equal(createdAt, room.CreatedAtUtc);
        Assert.NotNull(room.UpdatedAtUtc);
        Assert.InRange(room.UpdatedAtUtc.Value, beforeUpdate, DateTime.UtcNow);
        Assert.Equal([7, 11], room.RoomServices.Select(relation => relation.ServiceId));
        Assert.Contains(retainedRelation, room.RoomServices);
        Assert.DoesNotContain(removedRelation, room.RoomServices);
        Assert.Same(addedService, room.RoomServices.Single(relation => relation.ServiceId == 11).Service);
        Assert.Equal([removedService, retainedService, addedService], repository.Services);
        Assert.Equal(1, repository.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_ActiveRoom_PreservesHistoricalRelationsAndBookings()
    {
        var repository = new RoomRepository();
        var room = NewRoom();
        var relation = AddRelation(room, NewService(7));
        var booking = new Booking { Id = 15, RoomId = room.Id, Room = room, TotalPrice = 5000 };
        room.Bookings.Add(booking);
        repository.Rooms.Add(room);
        var service = new ConferenceRoomService(repository);
        var beforeDelete = DateTime.UtcNow;

        var deleted = await service.DeleteAsync(room.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.Same(room, Assert.Single(repository.Rooms));
        Assert.False(room.IsActive);
        Assert.NotNull(room.UpdatedAtUtc);
        Assert.InRange(room.UpdatedAtUtc.Value, beforeDelete, DateTime.UtcNow);
        Assert.Same(relation, Assert.Single(room.RoomServices));
        Assert.Same(booking, Assert.Single(room.Bookings));
        Assert.Equal(5000m, booking.TotalPrice);
        Assert.False(await service.DeleteAsync(room.Id, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(room.Id, CancellationToken.None));
        Assert.Empty(await service.GetAllAsync(CancellationToken.None));
        Assert.Equal(1, repository.SaveCount);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAndDeleteAsync_MissingOrInactiveRoom_ReturnNotFound(bool roomExists)
    {
        var repository = new RoomRepository();
        var room = NewRoom();
        room.IsActive = false;
        if (roomExists)
        {
            repository.Rooms.Add(room);
        }
        var service = new ConferenceRoomService(repository);

        var update = await service.UpdateAsync(room.Id, new UpdateConferenceRoomRequest
        {
            Name = "Changed room",
            Capacity = 30,
            BaseHourlyRate = 2000
        }, CancellationToken.None);

        Assert.True(update.IsNotFound);
        Assert.False(await service.DeleteAsync(room.Id, CancellationToken.None));
        Assert.Null(await service.GetByIdAsync(room.Id, CancellationToken.None));
        Assert.Null(room.UpdatedAtUtc);
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_CanceledRequest_DoesNotPersist()
    {
        var repository = new RoomRepository();
        var service = new ConferenceRoomService(repository);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.CreateAsync(
            new CreateConferenceRoomRequest { Name = "Meeting room", Capacity = 20, BaseHourlyRate = 1200 },
            cancellation.Token));

        Assert.Empty(repository.Rooms);
        Assert.Equal(0, repository.SaveCount);
    }

    private static ConferenceRoom NewRoom() => new()
    {
        Id = 42,
        Name = "Original room",
        Capacity = 10,
        BaseHourlyRate = 1000,
        IsActive = true,
        CreatedAtUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    };

    private static Service NewService(int id, bool isActive = true) => new()
    {
        Id = id,
        Name = $"Service {id}",
        Price = 100,
        IsActive = isActive
    };

    private static RoomService AddRelation(ConferenceRoom room, Service service)
    {
        var relation = new RoomService { RoomId = room.Id, Room = room, ServiceId = service.Id, Service = service };
        room.RoomServices.Add(relation);
        return relation;
    }

    private sealed class RoomRepository : IConferenceRoomRepository
    {
        public List<ConferenceRoom> Rooms { get; } = [];
        public List<Service> Services { get; } = [];
        public List<CancellationToken> Tokens { get; } = [];
        public int SaveCount { get; private set; }

        public Task<IReadOnlyList<ConferenceRoom>> GetAllAsync(CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            return Task.FromResult<IReadOnlyList<ConferenceRoom>>(Rooms.Where(room => room.IsActive).ToArray());
        }

        public Task<ConferenceRoom?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            return Task.FromResult(Rooms.SingleOrDefault(room => room.Id == id && room.IsActive));
        }

        public Task<ConferenceRoom?> GetForUpdateAsync(int id, CancellationToken cancellationToken)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<Service>> GetServicesByIdsAsync(
            IReadOnlyCollection<int> serviceIds, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            return Task.FromResult<IReadOnlyList<Service>>(
                Services.Where(service => serviceIds.Contains(service.Id)).ToArray());
        }

        public void Add(ConferenceRoom room) => Rooms.Add(room);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            SaveCount++;
            foreach (var room in Rooms.Where(room => room.Id == 0))
            {
                room.Id = Rooms.Max(existing => existing.Id) + 1;
            }
            return Task.CompletedTask;
        }

        private void Observe(CancellationToken cancellationToken)
        {
            Tokens.Add(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
