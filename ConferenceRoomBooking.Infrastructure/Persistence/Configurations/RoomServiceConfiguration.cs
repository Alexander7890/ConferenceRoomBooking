using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Configurations;

public class RoomServiceConfiguration : IEntityTypeConfiguration<RoomService>
{
    public void Configure(EntityTypeBuilder<RoomService> builder)
    {
        builder.ToTable("RoomServices");

        builder.HasKey(roomService => new { roomService.RoomId, roomService.ServiceId });

        builder.HasOne(roomService => roomService.Room)
            .WithMany(room => room.RoomServices)
            .HasForeignKey(roomService => roomService.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(roomService => roomService.Service)
            .WithMany(service => service.RoomServices)
            .HasForeignKey(roomService => roomService.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
