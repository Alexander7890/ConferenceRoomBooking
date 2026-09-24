using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint("CK_Bookings_TotalPrice", "`TotalPrice` >= 0");
            table.HasCheckConstraint("CK_Bookings_DateRange", "`EndDateTime` > `StartDateTime`");
        });

        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.StartDateTime).IsRequired();
        builder.Property(booking => booking.EndDateTime).IsRequired();
        builder.Property(booking => booking.TotalPrice).HasPrecision(18, 2).IsRequired();
        builder.Property(booking => booking.CreatedAtUtc).IsRequired();

        builder.HasOne(booking => booking.Room)
            .WithMany(room => room.Bookings)
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(booking => new { booking.RoomId, booking.StartDateTime, booking.EndDateTime });
    }
}
