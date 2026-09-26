namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public sealed class CreateBookingRequest
{
    public int RoomId { get; init; }
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public IReadOnlyCollection<int> ServiceIds { get; init; } = [];
}
