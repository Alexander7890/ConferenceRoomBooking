namespace ConferenceRoomBooking.Application.DTOs.Reports;

public sealed class ReportPeriodRequest
{
    public DateTime From { get; init; }
    public DateTime To { get; init; }
}
