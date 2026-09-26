using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Domain.Entities;
using BookingApplicationService = ConferenceRoomBooking.Application.Services.BookingService;

namespace ConferenceRoomBooking.Tests.Bookings;

public sealed class BookingServiceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsync_RejectsMissingOrInactiveRoom(bool roomExists)
    {
        var repository = new BookingRepositoryStub { Room = roomExists ? CreateRoom(isActive: false) : null };
        var service = new BookingApplicationService(repository);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => service.CreateAsync(Request(), CancellationToken.None));

        Assert.Null(repository.SavedBooking);
    }

    [Fact]
    public async Task CreateAsync_RejectsUnknownOrUnavailableService()
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom() };
        var service = new BookingApplicationService(repository);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(Request(serviceIds: [99]), CancellationToken.None));

        Assert.Null(repository.SavedBooking);
    }

    [Fact]
    public async Task CreateAsync_RejectsInactiveService()
    {
        var room = CreateRoom();
        room.RoomServices.Single().Service.IsActive = false;
        var repository = new BookingRepositoryStub { Room = room };
        var service = new BookingApplicationService(repository);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(Request(serviceIds: [11]), CancellationToken.None));

        Assert.Null(repository.SavedBooking);
    }

    [Theory]
    [InlineData(11, 11)]
    [InlineData(0, 11)]
    [InlineData(-1, 11)]
    public async Task CreateAsync_RejectsDuplicateOrInvalidServiceIdsBeforePersistence(int firstId, int secondId)
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom() };
        var service = new BookingApplicationService(repository);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.CreateAsync(Request(serviceIds: [firstId, secondId]), CancellationToken.None));

        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsNullServiceIdsBeforePersistence()
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom() };
        var service = new BookingApplicationService(repository);
        var request = new CreateBookingRequest
        {
            RoomId = 7,
            StartDateTime = new DateTime(2026, 10, 1, 10, 0, 0),
            EndDateTime = new DateTime(2026, 10, 1, 11, 0, 0),
            ServiceIds = null!
        };

        await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(request, CancellationToken.None));

        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task CreateAsync_RejectsOverlap()
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom(), HasOverlap = true };
        var service = new BookingApplicationService(repository);

        await Assert.ThrowsAsync<BookingConflictException>(() => service.CreateAsync(Request(), CancellationToken.None));

        Assert.Null(repository.SavedBooking);
    }

    [Fact]
    public async Task CreateAsync_SavesValidBookingAndHistoricalPrices()
    {
        var room = CreateRoom();
        var repository = new BookingRepositoryStub { Room = room };
        var service = new BookingApplicationService(repository);
        using var source = new CancellationTokenSource();
        var before = DateTime.UtcNow;

        var result = await service.CreateAsync(Request(serviceIds: [11]), source.Token);

        var saved = Assert.IsType<Booking>(repository.SavedBooking);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(41, result.BookingId);
        Assert.Equal(7, result.Room.Id);
        Assert.Equal("Meeting room", result.Room.Name);
        Assert.Equal(2000m, result.RentalPrice);
        Assert.Equal(500m, result.ServicesPrice);
        Assert.Equal(2500m, result.TotalPrice);
        Assert.InRange(saved.CreatedAtUtc, before, DateTime.UtcNow);
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAtUtc.Kind);
        var relation = Assert.Single(saved.BookingServices);
        Assert.Equal(500m, relation.PriceAtBooking);
        Assert.Equal(11, relation.ServiceId);
        Assert.Same(saved, relation.Booking);

        room.BaseHourlyRate = 3000m;
        room.RoomServices.Single().Service.Price = 700m;

        Assert.Equal(2500m, saved.TotalPrice);
        Assert.Equal(500m, relation.PriceAtBooking);
        Assert.Equal(500m, Assert.Single(result.SelectedServices).Price);
    }

    [Fact]
    public async Task CreateAsync_AllowsBookingWithoutServices()
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom() };
        var service = new BookingApplicationService(repository);

        var result = await service.CreateAsync(Request(), CancellationToken.None);

        Assert.Empty(result.SelectedServices);
        Assert.Equal(2000m, result.TotalPrice);
        Assert.Equal(0m, result.ServicesPrice);
    }

    [Fact]
    public async Task CreateAsync_RejectsTotalOutsideDatabasePrecision()
    {
        var room = CreateRoom();
        room.BaseHourlyRate = 9999999999999999.99m;
        var repository = new BookingRepositoryStub { Room = room };
        var service = new BookingApplicationService(repository);
        var request = Request(serviceIds: [11]);

        await Assert.ThrowsAsync<RequestValidationException>(() => service.CreateAsync(request, CancellationToken.None));

        Assert.Null(repository.SavedBooking);
    }

    [Fact]
    public async Task CreateAsync_ForwardsCancellation()
    {
        var repository = new BookingRepositoryStub { Room = CreateRoom() };
        var service = new BookingApplicationService(repository);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => service.CreateAsync(Request(), source.Token));

        Assert.Null(repository.SavedBooking);
    }

    private static CreateBookingRequest Request(IReadOnlyCollection<int>? serviceIds = null) => new()
    {
        RoomId = 7,
        StartDateTime = new DateTime(2026, 10, 1, 10, 0, 0),
        EndDateTime = new DateTime(2026, 10, 1, 11, 0, 0),
        ServiceIds = serviceIds ?? []
    };

    private static ConferenceRoom CreateRoom(bool isActive = true)
    {
        var room = new ConferenceRoom
        {
            Id = 7,
            Name = "Meeting room",
            Capacity = 50,
            BaseHourlyRate = 2000m,
            IsActive = isActive
        };
        var projector = new Service { Id = 11, Name = "Projector", Price = 500m, IsActive = true };
        room.RoomServices.Add(new RoomService { RoomId = room.Id, Room = room, ServiceId = projector.Id, Service = projector });
        return room;
    }

    private sealed class BookingRepositoryStub : IBookingRepository
    {
        public ConferenceRoom? Room { get; init; }
        public bool HasOverlap { get; init; }
        public Booking? SavedBooking { get; private set; }
        public CancellationToken Token { get; private set; }
        public int Calls { get; private set; }

        public Task<Booking> CreateAsync(
            int roomId,
            DateTime startDateTime,
            DateTime endDateTime,
            Func<ConferenceRoom?, bool, Booking> createBooking,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Token = cancellationToken;
            Calls++;
            var booking = createBooking(Room, HasOverlap);
            booking.Id = 41;
            SavedBooking = booking;
            return Task.FromResult(booking);
        }
    }
}
