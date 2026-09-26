using ConferenceRoomBooking.Application.DTOs.Reports;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IReportService
{
    Task<RevenueReportResponse> GetRevenueAsync(ReportPeriodRequest period, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomReportResponse>> GetRoomsAsync(ReportPeriodRequest period, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceReportResponse>> GetServicesAsync(ReportPeriodRequest period, CancellationToken cancellationToken);
}
