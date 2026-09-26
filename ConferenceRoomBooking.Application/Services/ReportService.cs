using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;

namespace ConferenceRoomBooking.Application.Services;

public sealed class ReportService(IReportRepository repository) : IReportService
{
    public Task<RevenueReportResponse> GetRevenueAsync(ReportPeriodRequest period, CancellationToken cancellationToken)
    {
        ValidatePeriod(period);
        return repository.GetRevenueAsync(period.From, period.To, cancellationToken);
    }

    public Task<IReadOnlyList<RoomReportResponse>> GetRoomsAsync(ReportPeriodRequest period, CancellationToken cancellationToken)
    {
        ValidatePeriod(period);
        return repository.GetRoomsAsync(period.From, period.To, cancellationToken);
    }

    public Task<IReadOnlyList<ServiceReportResponse>> GetServicesAsync(ReportPeriodRequest period, CancellationToken cancellationToken)
    {
        ValidatePeriod(period);
        return repository.GetServicesAsync(period.From, period.To, cancellationToken);
    }

    private static void ValidatePeriod(ReportPeriodRequest period)
    {
        var errors = new Dictionary<string, string[]>();
        if (period.From.Year < 1000)
        {
            errors[nameof(period.From)] = ["From is required."];
        }

        if (period.To.Year < 1000 || period.To <= period.From)
        {
            errors[nameof(period.To)] = ["To must be later than From."];
        }

        if (period.From.Kind != DateTimeKind.Unspecified || period.To.Kind != DateTimeKind.Unspecified)
        {
            errors[nameof(period.From)] = ["Use local room times without a UTC suffix or time zone offset."];
        }

        if (period.From.Ticks % 10 != 0 || period.To.Ticks % 10 != 0)
        {
            errors[nameof(period.From)] = ["Report dates support at most six fractional second digits."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
