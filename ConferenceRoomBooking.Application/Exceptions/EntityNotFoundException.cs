namespace ConferenceRoomBooking.Application.Exceptions;

public sealed class EntityNotFoundException(string message) : Exception(message);
