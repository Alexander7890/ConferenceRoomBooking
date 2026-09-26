using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Services;

namespace ConferenceRoomBooking.Tests.Rooms;

public sealed class AvailabilityServiceTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task FindAsync_InvalidCapacity_DoesNotQueryRooms(int capacity)
    {
        var service = new AvailabilityService(new UnusedRepository());

        var exception = await Assert.ThrowsAsync<RequestValidationException>(() => service.FindAsync(
            new RoomAvailabilityRequest
            {
                StartDateTime = new DateTime(2026, 10, 1, 10, 0, 0),
                EndDateTime = new DateTime(2026, 10, 1, 12, 0, 0),
                Capacity = capacity
            }, CancellationToken.None));

        Assert.Contains(nameof(RoomAvailabilityRequest.Capacity), exception.Errors.Keys);
    }

    [Theory]
    [InlineData(12, 10)]
    [InlineData(10, 10)]
    [InlineData(5, 10)]
    [InlineData(22, 23.5)]
    public async Task FindAsync_InvalidTimeRange_DoesNotQueryRooms(double startHour, double endHour)
    {
        var service = new AvailabilityService(new UnusedRepository());
        var date = new DateTime(2026, 10, 1);

        await Assert.ThrowsAsync<RequestValidationException>(() => service.FindAsync(
            new RoomAvailabilityRequest
            {
                StartDateTime = date.AddHours(startHour),
                EndDateTime = date.AddHours(endHour),
                Capacity = 1
            }, CancellationToken.None));
    }

    private sealed class UnusedRepository : IAvailabilityRepository
    {
        public Task<IReadOnlyList<ConferenceRoomResponse>> FindAsync(
            DateTime startDateTime, DateTime endDateTime, int capacity, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Invalid requests must be rejected before querying rooms.");
    }
}
