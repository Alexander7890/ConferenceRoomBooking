using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/reports")]
[ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
    Description = "The report period is invalid. Use local times without a UTC suffix or offset, with To later than From.")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json",
    Description = "An unexpected server error occurred.")]
public sealed class ReportsController(IReportService reportService) : ControllerBase
{
    [HttpGet("revenue")]
    [EndpointSummary("Get booking revenue totals")]
    [EndpointDescription("Returns total revenue, booking count and average booking value for bookings starting " +
        "at or after From and before To. Revenue uses saved booking totals, including services, and represents " +
        "booked value rather than received payments. Historical bookings remain included when a room is inactive.")]
    [ProducesResponseType<RevenueReportResponse>(StatusCodes.Status200OK, "application/json",
        Description = "The period totals in UAH, or zero values when there are no bookings.")]
    public async Task<ActionResult<RevenueReportResponse>> GetRevenue(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetRevenueAsync(period, cancellationToken));

    [HttpGet("rooms")]
    [EndpointSummary("Get conference room usage and revenue")]
    [EndpointDescription("Returns booking count, full booked hours and revenue for each room, including inactive rooms " +
        "and rooms with no bookings. Includes bookings starting at or after From and before To. " +
        "Revenue uses the full historical booking total, including selected services.")]
    [ProducesResponseType<IReadOnlyList<RoomReportResponse>>(StatusCodes.Status200OK, "application/json",
        Description = "Usage and revenue in UAH for each conference room.")]
    public async Task<ActionResult<IReadOnlyList<RoomReportResponse>>> GetRooms(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetRoomsAsync(period, cancellationToken));

    [HttpGet("services")]
    [EndpointSummary("Get service usage and revenue")]
    [EndpointDescription("Returns selection count and revenue for each service, including inactive and unused services. " +
        "Includes bookings starting at or after From and before To. Revenue uses prices saved at booking time, " +
        "so later catalog price changes do not alter the report.")]
    [ProducesResponseType<IReadOnlyList<ServiceReportResponse>>(StatusCodes.Status200OK, "application/json",
        Description = "Selection counts and historical service revenue in UAH.")]
    public async Task<ActionResult<IReadOnlyList<ServiceReportResponse>>> GetServices(
        [FromQuery] ReportPeriodRequest period, CancellationToken cancellationToken)
        => Ok(await reportService.GetServicesAsync(period, cancellationToken));
}
