namespace ConferenceRoomBooking.Domain.Entities;

public class ConferenceRoom
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int Capacity { get; set; }
    public decimal BaseHourlyRate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<RoomService> RoomServices { get; set; } = [];
    public ICollection<Booking> Bookings { get; set; } = [];
}
