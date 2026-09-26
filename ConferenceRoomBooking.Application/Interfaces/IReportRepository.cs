using ConferenceRoomBooking.Application.DTOs.Reports;

namespace ConferenceRoomBooking.Application.Interfaces;

public interface IReportRepository
{
    Task<RevenueReportResponse> GetRevenueAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomReportResponse>> GetRoomsAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
    Task<IReadOnlyList<ServiceReportResponse>> GetServicesAsync(DateTime from, DateTime to, CancellationToken cancellationToken);
}
