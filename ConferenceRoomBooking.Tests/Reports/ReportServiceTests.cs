using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Services;

namespace ConferenceRoomBooking.Tests.Reports;

public sealed class ReportServiceTests
{
    [Theory]
    [InlineData("0001-01-01", "2026-10-02")]
    [InlineData("2026-10-01", "0001-01-01")]
    [InlineData("2026-10-01", "2026-10-01")]
    [InlineData("2026-10-02", "2026-10-01")]
    public async Task InvalidPeriod_IsRejectedForEveryReport(string from, string to)
    {
        var service = new ReportService(new UnusedRepository());
        var period = new ReportPeriodRequest
        {
            From = DateTime.Parse(from, System.Globalization.CultureInfo.InvariantCulture),
            To = DateTime.Parse(to, System.Globalization.CultureInfo.InvariantCulture)
        };

        await Assert.ThrowsAsync<RequestValidationException>(() => service.GetRevenueAsync(period, CancellationToken.None));
        await Assert.ThrowsAsync<RequestValidationException>(() => service.GetRoomsAsync(period, CancellationToken.None));
        await Assert.ThrowsAsync<RequestValidationException>(() => service.GetServicesAsync(period, CancellationToken.None));
    }

    [Fact]
    public async Task UtcPeriod_IsRejectedToKeepReportsConsistentWithRoomTimes()
    {
        var service = new ReportService(new UnusedRepository());
        var period = new ReportPeriodRequest
        {
            From = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            To = new DateTime(2026, 10, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        await Assert.ThrowsAsync<RequestValidationException>(() => service.GetRevenueAsync(period, CancellationToken.None));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(0, 1)]
    public async Task SubMicrosecondPeriod_IsRejected(int fromTicks, int toTicks)
    {
        var service = new ReportService(new UnusedRepository());
        var period = new ReportPeriodRequest
        {
            From = new DateTime(2026, 10, 1).AddTicks(fromTicks),
            To = new DateTime(2026, 10, 2).AddTicks(toTicks)
        };

        var exception = await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetRevenueAsync(period, CancellationToken.None));

        Assert.Contains("six fractional second digits", exception.Errors[nameof(period.From)][0]);
    }

    private sealed class UnusedRepository : IReportRepository
    {
        public Task<RevenueReportResponse> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Invalid periods must be rejected before querying reports.");

        public Task<IReadOnlyList<RoomReportResponse>> GetRoomsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Invalid periods must be rejected before querying reports.");

        public Task<IReadOnlyList<ServiceReportResponse>> GetServicesAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
            => throw new InvalidOperationException("Invalid periods must be rejected before querying reports.");
    }
}
