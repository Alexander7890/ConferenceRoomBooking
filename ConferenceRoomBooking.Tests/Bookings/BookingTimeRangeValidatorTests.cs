using ConferenceRoomBooking.Application.Validation;

namespace ConferenceRoomBooking.Tests.Bookings;

public sealed class BookingTimeRangeValidatorTests
{
    [Theory]
    [InlineData("2026-10-01T06:00:00", "2026-10-01T23:00:00", true)]
    [InlineData("2026-10-01T10:00:00", "2026-10-01T10:30:00", true)]
    [InlineData("2026-10-01T05:59:00", "2026-10-01T07:00:00", false)]
    [InlineData("2026-10-01T22:00:00", "2026-10-01T23:01:00", false)]
    [InlineData("2026-10-01T10:00:00", "2026-10-01T10:00:00", false)]
    [InlineData("2026-10-01T11:00:00", "2026-10-01T10:00:00", false)]
    [InlineData("2026-10-01T22:00:00", "2026-10-02T07:00:00", false)]
    [InlineData("0001-01-01T10:00:00", "0001-01-01T11:00:00", false)]
    public void Validate_EnforcesSingleDayOpeningHours(string start, string end, bool isValid)
    {
        var errors = BookingTimeRangeValidator.Validate(DateTime.Parse(start), DateTime.Parse(end));

        Assert.Equal(isValid, errors.Count == 0);
    }

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    public void Validate_RejectsTimeZoneConversions(DateTimeKind kind)
    {
        var start = new DateTime(2026, 10, 1, 10, 0, 0, kind);

        Assert.NotEmpty(BookingTimeRangeValidator.Validate(start, start.AddHours(1)));
    }

    [Fact]
    public void Validate_RejectsPrecisionThatMySqlCannotPreserve()
    {
        var start = new DateTime(2026, 10, 1, 10, 0, 0).AddTicks(1);

        Assert.NotEmpty(BookingTimeRangeValidator.Validate(start, start.AddHours(1)));
    }
}
