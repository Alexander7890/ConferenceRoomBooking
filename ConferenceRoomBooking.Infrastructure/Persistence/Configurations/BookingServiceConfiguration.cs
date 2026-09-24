using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Configurations;

public class BookingServiceConfiguration : IEntityTypeConfiguration<BookingService>
{
    public void Configure(EntityTypeBuilder<BookingService> builder)
    {
        builder.ToTable("BookingServices", table =>
            table.HasCheckConstraint("CK_BookingServices_PriceAtBooking", "`PriceAtBooking` >= 0"));

        builder.HasKey(bookingService => new { bookingService.BookingId, bookingService.ServiceId });
        builder.Property(bookingService => bookingService.PriceAtBooking).HasPrecision(18, 2).IsRequired();

        builder.HasOne(bookingService => bookingService.Booking)
            .WithMany(booking => booking.BookingServices)
            .HasForeignKey(bookingService => bookingService.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(bookingService => bookingService.Service)
            .WithMany(service => service.BookingServices)
            .HasForeignKey(bookingService => bookingService.ServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
