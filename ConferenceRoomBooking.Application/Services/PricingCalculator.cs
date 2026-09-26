using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Validation;

namespace ConferenceRoomBooking.Application.Services;

public static class PricingCalculator
{
    private static readonly (int StartHour, int EndHour, decimal Multiplier)[] PricePeriods =
    [
        (6, 9, 0.90m),
        (9, 12, 1.00m),
        (12, 14, 1.15m),
        (14, 18, 1.00m),
        (18, 23, 0.80m)
    ];

    public static BookingPrice Calculate(
        decimal baseHourlyRate,
        DateTime startDateTime,
        DateTime endDateTime,
        IReadOnlyCollection<decimal> servicePrices)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(baseHourlyRate);
        if (BookingTimeRangeValidator.Validate(startDateTime, endDateTime).Count > 0)
        {
            throw new ArgumentException("The booking time range is invalid.", nameof(endDateTime));
        }

        decimal rentalPrice = 0;
        foreach (var period in PricePeriods)
        {
            var periodStart = startDateTime.Date.AddHours(period.StartHour);
            var periodEnd = startDateTime.Date.AddHours(period.EndHour);
            var segmentStart = startDateTime > periodStart ? startDateTime : periodStart;
            var segmentEnd = endDateTime < periodEnd ? endDateTime : periodEnd;
            if (segmentStart >= segmentEnd)
            {
                continue;
            }

            var hours = (decimal)(segmentEnd - segmentStart).Ticks / TimeSpan.TicksPerHour;
            rentalPrice += hours * baseHourlyRate * period.Multiplier;
        }

        return new BookingPrice(
            decimal.Round(rentalPrice, 2, MidpointRounding.AwayFromZero), servicePrices.Sum());
    }
}
