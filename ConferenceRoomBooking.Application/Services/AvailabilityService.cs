using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Validation;

namespace ConferenceRoomBooking.Application.Services;

public sealed class AvailabilityService(IAvailabilityRepository repository) : IAvailabilityService
{
    public Task<IReadOnlyList<ConferenceRoomResponse>> FindAsync(
        RoomAvailabilityRequest request, CancellationToken cancellationToken)
    {
        var errors = BookingTimeRangeValidator.Validate(request.StartDateTime, request.EndDateTime);
        if (request.Capacity <= 0)
        {
            errors[nameof(request.Capacity)] = ["Capacity must be greater than zero."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }

        return repository.FindAsync(request.StartDateTime, request.EndDateTime, request.Capacity, cancellationToken);
    }
}
