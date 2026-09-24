namespace ConferenceRoomBooking.Domain.Entities;

public class BookingService
{
    public int BookingId { get; set; }
    public required Booking Booking { get; set; }

    public int ServiceId { get; set; }
    public required Service Service { get; set; }

    public decimal PriceAtBooking { get; set; }
}
