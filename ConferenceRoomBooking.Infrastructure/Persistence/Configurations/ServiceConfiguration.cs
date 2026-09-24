using ConferenceRoomBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomBooking.Infrastructure.Persistence.Configurations;

public class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services", table =>
            table.HasCheckConstraint("CK_Services_Price", "`Price` >= 0"));

        builder.HasKey(service => service.Id);
        builder.Property(service => service.Name).HasMaxLength(200).IsRequired();
        builder.Property(service => service.Price).HasPrecision(18, 2).IsRequired();
        builder.Property(service => service.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasIndex(service => service.Name).IsUnique();
    }
}
