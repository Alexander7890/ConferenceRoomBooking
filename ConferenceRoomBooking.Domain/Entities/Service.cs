namespace ConferenceRoomBooking.Domain.Entities;

public class Service
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; }

    public ICollection<RoomService> RoomServices { get; set; } = [];
    public ICollection<BookingService> BookingServices { get; set; } = [];
}
