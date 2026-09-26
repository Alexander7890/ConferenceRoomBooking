using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetRevenueAsync(period, cancellationToken));

    [HttpGet("rooms")]
    public async Task<ActionResult<IReadOnlyList<RoomReportResponse>>> GetRooms(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetRoomsAsync(period, cancellationToken));

    [HttpGet("services")]
    public async Task<ActionResult<IReadOnlyList<ServiceReportResponse>>> GetServices(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetServicesAsync(period, cancellationToken));
}
