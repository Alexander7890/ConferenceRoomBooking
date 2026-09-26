using System.Data;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Repositories;

public sealed class BookingRepository(AppDbContext dbContext) : IBookingRepository
{
    public async Task<Booking> CreateAsync(
        int roomId,
        DateTime startDateTime,
        DateTime endDateTime,
        Func<ConferenceRoom?, bool, Booking> createBooking,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted, cancellationToken);

        // Lock the parent even when no bookings exist; each waiting request then sees the previous commit.
        var rooms = await dbContext.ConferenceRooms
            .FromSqlInterpolated($"SELECT * FROM `ConferenceRooms` WHERE `Id` = {roomId} FOR UPDATE")
            .ToListAsync(cancellationToken);
        var room = rooms.SingleOrDefault();
        if (room is not null)
        {
            await dbContext.Entry(room).Collection(entity => entity.RoomServices)
                .Query()
                .Include(relation => relation.Service)
                .LoadAsync(cancellationToken);
        }

        var hasOverlap = await dbContext.Bookings.AnyAsync(
            booking => booking.RoomId == roomId &&
                startDateTime < booking.EndDateTime && endDateTime > booking.StartDateTime,
            cancellationToken);
        var booking = createBooking(room, hasOverlap);
        dbContext.Bookings.Add(booking);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return booking;
    }
}
