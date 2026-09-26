namespace ConferenceRoomBooking.Application.DTOs.Reports;

public sealed record RevenueReportResponse(decimal TotalRevenue, int BookingCount, decimal AverageBookingValue);
