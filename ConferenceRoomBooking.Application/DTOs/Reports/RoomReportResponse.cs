namespace ConferenceRoomBooking.Application.DTOs.Reports;

public sealed record RoomReportResponse(
    int RoomId, string RoomName, int BookingCount, decimal BookedHours, decimal Revenue);
