namespace ConferenceRoomBooking.Application.DTOs.Bookings;

public sealed record BookingResponse(
    int BookingId,
    BookingRoomResponse Room,
    DateTime StartDateTime,
    DateTime EndDateTime,
    IReadOnlyList<BookingServiceResponse> SelectedServices,
    decimal RentalPrice,
    decimal ServicesPrice,
    decimal TotalPrice);

public sealed record BookingRoomResponse(int Id, string Name);

public sealed record BookingServiceResponse(int Id, string Name, decimal Price);
