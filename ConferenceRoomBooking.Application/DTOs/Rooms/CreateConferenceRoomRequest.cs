namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public sealed class CreateConferenceRoomRequest
{
    public string Name { get; init; } = string.Empty;
    public int Capacity { get; init; }
    public decimal BaseHourlyRate { get; init; }
    public IReadOnlyCollection<int> ServiceIds { get; init; } = [];
}
