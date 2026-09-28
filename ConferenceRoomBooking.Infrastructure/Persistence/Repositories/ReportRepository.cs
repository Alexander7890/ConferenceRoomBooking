using ConferenceRoomBooking.Application.DTOs.Reports;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Repositories;

public sealed class ReportRepository(AppDbContext dbContext) : IReportRepository
{
    public async Task<RevenueReportResponse> GetRevenueAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        // Include bookings by their start time, with an exclusive upper bound to avoid counting them twice.
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(booking => booking.StartDateTime >= from && booking.StartDateTime < to)
            .GroupBy(booking => 1)
            .Select(group => new RevenueReportResponse(
                group.Sum(booking => booking.TotalPrice),
                group.Count(),
                group.Average(booking => booking.TotalPrice)))
            .SingleOrDefaultAsync(cancellationToken) ?? new RevenueReportResponse(0m, 0, 0m);
    }

    public async Task<IReadOnlyList<RoomReportResponse>> GetRoomsAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        // Keep inactive rooms in historical reports and use the full saved booking total.
        return await dbContext.ConferenceRooms
            .AsNoTracking()
            .OrderBy(room => room.Id)
            .Select(room => new RoomReportResponse(
                room.Id,
                room.Name,
                room.Bookings.Count(booking => booking.StartDateTime >= from && booking.StartDateTime < to),
                room.Bookings
                    .Where(booking => booking.StartDateTime >= from && booking.StartDateTime < to)
                    .Sum(booking => (decimal)EF.Functions.DateDiffMicrosecond(
                        booking.StartDateTime, booking.EndDateTime) / 3_600_000_000m),
                room.Bookings
                    .Where(booking => booking.StartDateTime >= from && booking.StartDateTime < to)
                    .Sum(booking => booking.TotalPrice)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceReportResponse>> GetServicesAsync(
        DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        // Revenue comes from booked prices, not the current service catalog.
        return await dbContext.Services
            .AsNoTracking()
            .OrderBy(service => service.Id)
            .Select(service => new ServiceReportResponse(
                service.Id,
                service.Name,
                service.BookingServices.Count(relation =>
                    relation.Booking.StartDateTime >= from && relation.Booking.StartDateTime < to),
                service.BookingServices
                    .Where(relation => relation.Booking.StartDateTime >= from && relation.Booking.StartDateTime < to)
                    .Sum(relation => relation.PriceAtBooking)))
            .ToListAsync(cancellationToken);
    }
}
