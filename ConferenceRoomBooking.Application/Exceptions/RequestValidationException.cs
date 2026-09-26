namespace ConferenceRoomBooking.Application.Exceptions;

public sealed class RequestValidationException(Dictionary<string, string[]> errors)
    : Exception("One or more validation errors occurred.")
{
    public Dictionary<string, string[]> Errors { get; } = errors;
}
