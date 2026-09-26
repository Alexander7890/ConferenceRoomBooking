using ConferenceRoomBooking.Application.DTOs.Rooms;

namespace ConferenceRoomBooking.Application.Validation;

internal static class ConferenceRoomRequestValidator
{
    private const int MaximumNameLength = 200;
    private const decimal MaximumHourlyRate = 9_999_999_999_999_999.99m;
    private const int HourlyRateScale = 2;

    public static Dictionary<string, string[]> Validate(
        string name, int capacity, decimal baseHourlyRate, IReadOnlyCollection<int> serviceIds)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors[nameof(CreateConferenceRoomRequest.Name)] = ["Name is required."];
        }
        else if (name.Length > MaximumNameLength)
        {
            errors[nameof(CreateConferenceRoomRequest.Name)] =
                [$"Name must not exceed {MaximumNameLength} characters."];
        }

        if (capacity <= 0)
        {
            errors[nameof(CreateConferenceRoomRequest.Capacity)] = ["Capacity must be greater than zero."];
        }

        if (baseHourlyRate <= 0 || baseHourlyRate > MaximumHourlyRate)
        {
            errors[nameof(CreateConferenceRoomRequest.BaseHourlyRate)] =
                [$"BaseHourlyRate must be greater than zero and no greater than {MaximumHourlyRate}."];
        }
        else if (decimal.Round(baseHourlyRate, HourlyRateScale) != baseHourlyRate)
        {
            errors[nameof(CreateConferenceRoomRequest.BaseHourlyRate)] =
                [$"BaseHourlyRate must have at most {HourlyRateScale} decimal places."];
        }

        if (serviceIds is null)
        {
            errors[nameof(CreateConferenceRoomRequest.ServiceIds)] = ["ServiceIds must not be null."];
        }
        else if (serviceIds.Any(id => id <= 0))
        {
            errors[nameof(CreateConferenceRoomRequest.ServiceIds)] = ["Service IDs must be positive."];
        }
        else if (serviceIds.Distinct().Count() != serviceIds.Count)
        {
            errors[nameof(CreateConferenceRoomRequest.ServiceIds)] = ["ServiceIds must not contain duplicates."];
        }

        return errors;
    }
}
