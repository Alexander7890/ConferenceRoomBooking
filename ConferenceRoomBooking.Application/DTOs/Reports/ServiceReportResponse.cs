namespace ConferenceRoomBooking.Application.DTOs.Reports;

public sealed record ServiceReportResponse(int ServiceId, string ServiceName, int TimesSelected, decimal Revenue);
