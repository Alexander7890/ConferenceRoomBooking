namespace ConferenceRoomBooking.Domain.Entities;

public class Booking
{
    public int Id { get; set; }

    public int RoomId { get; set; }
    public required ConferenceRoom Room { get; set; }

    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<BookingService> BookingServices { get; set; } = [];
}
