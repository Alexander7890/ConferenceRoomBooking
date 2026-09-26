namespace ConferenceRoomBooking.Application.Validation;

public static class BookingTimeRangeValidator
{
    public static Dictionary<string, string[]> Validate(DateTime startDateTime, DateTime endDateTime)
    {
        var errors = new Dictionary<string, string[]>();

        if (startDateTime.Kind != DateTimeKind.Unspecified || endDateTime.Kind != DateTimeKind.Unspecified)
        {
            errors["StartDateTime"] = ["Use local room times without a UTC suffix or time zone offset."];
        }

        if (startDateTime.Year < 1000 || endDateTime.Year < 1000)
        {
            errors["StartDateTime"] = ["A valid start and end date are required."];
        }

        if (startDateTime.Ticks % 10 != 0 || endDateTime.Ticks % 10 != 0)
        {
            errors["StartDateTime"] = ["Times support at most six fractional second digits."];
        }

        if (endDateTime <= startDateTime)
        {
            errors["EndDateTime"] = ["End time must be later than start time."];
        }
        else if (startDateTime.Date != endDateTime.Date)
        {
            errors["EndDateTime"] = ["Bookings must start and end on the same calendar day."];
        }

        if (startDateTime.TimeOfDay < TimeSpan.FromHours(6) ||
            endDateTime.TimeOfDay > TimeSpan.FromHours(23))
        {
            errors["StartDateTime"] = ["Bookings are available from 06:00 to 23:00."];
        }

        return errors;
    }
}
