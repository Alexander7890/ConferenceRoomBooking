namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public sealed class RoomAvailabilityRequest
{
    public DateTime StartDateTime { get; init; }
    public DateTime EndDateTime { get; init; }
    public int Capacity { get; init; }
}
