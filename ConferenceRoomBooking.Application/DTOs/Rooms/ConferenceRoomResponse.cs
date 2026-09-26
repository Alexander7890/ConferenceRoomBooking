namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public sealed record ConferenceRoomResponse(
    int Id,
    string Name,
    int Capacity,
    decimal BaseHourlyRate,
    bool IsActive,
    IReadOnlyCollection<ConferenceRoomServiceResponse> Services);
