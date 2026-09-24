using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Configurations;

public class ConferenceRoomConfiguration : IEntityTypeConfiguration<ConferenceRoom>
{
    public void Configure(EntityTypeBuilder<ConferenceRoom> builder)
    {
        builder.ToTable("ConferenceRooms", table =>
        {
            table.HasCheckConstraint("CK_ConferenceRooms_Capacity", "`Capacity` > 0");
            table.HasCheckConstraint("CK_ConferenceRooms_BaseHourlyRate", "`BaseHourlyRate` > 0");
        });

        builder.HasKey(room => room.Id);
        builder.Property(room => room.Name).HasMaxLength(200).IsRequired();
        builder.Property(room => room.Capacity).IsRequired();
        builder.Property(room => room.BaseHourlyRate).HasPrecision(18, 2).IsRequired();
        builder.Property(room => room.IsActive).HasDefaultValue(true).IsRequired();
        builder.Property(room => room.CreatedAtUtc).IsRequired();
        builder.Property(room => room.UpdatedAtUtc).IsRequired(false);
    }
}
