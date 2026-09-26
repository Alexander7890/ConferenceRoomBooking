using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ConferenceRoomBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IConferenceRoomService, ConferenceRoomService>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IReportService, ReportService>();
        return services;
    }
}
