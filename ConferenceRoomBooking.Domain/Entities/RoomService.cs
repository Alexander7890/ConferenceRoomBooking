namespace ConferenceRoomBooking.Domain.Entities;

public class RoomService
{
    public int RoomId { get; set; }
    public required ConferenceRoom Room { get; set; }

    public int ServiceId { get; set; }
    public required Service Service { get; set; }
}
