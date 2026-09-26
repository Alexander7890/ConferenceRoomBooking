namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public sealed record BookingPrice(decimal RentalPrice, decimal ServicesPrice)
{
    public decimal TotalPrice => RentalPrice + ServicesPrice;
}
