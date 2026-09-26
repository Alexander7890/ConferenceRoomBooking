namespace ConferenceRoomBooking.Application.DTOs.Rooms;

public sealed record ConferenceRoomWriteResult(
    ConferenceRoomResponse? Room,
    Dictionary<string, string[]> Errors)
{
    public bool IsNotFound => Room is null && Errors.Count == 0;

    public static ConferenceRoomWriteResult Success(ConferenceRoomResponse room) => new(room, []);

    public static ConferenceRoomWriteResult Invalid(Dictionary<string, string[]> errors) => new(null, errors);

    public static ConferenceRoomWriteResult NotFound() => new(null, []);
}
