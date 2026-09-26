using ConferenceRoomBooking.Application.DTOs.Bookings;
using ConferenceRoomBooking.Application.Exceptions;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Validation;
using ConferenceRoomBooking.Domain.Entities;
using BookingServiceEntity = ConferenceRoomBooking.Domain.Entities.BookingService;

namespace ConferenceRoomBooking.Application.Services;

public sealed class BookingService(IBookingRepository repository) : IBookingService
{
    private const decimal MaximumTotalPrice = 9999999999999999.99m;

    public async Task<BookingResponse> CreateAsync(CreateBookingRequest request, CancellationToken cancellationToken)
    {
        var errors = BookingTimeRangeValidator.Validate(request.StartDateTime, request.EndDateTime);
        if (request.RoomId <= 0)
        {
            errors[nameof(request.RoomId)] = ["Room ID must be greater than zero."];
        }

        if (request.ServiceIds is null || request.ServiceIds.Any(id => id <= 0) ||
            request.ServiceIds.Distinct().Count() != request.ServiceIds.Count)
        {
            errors[nameof(request.ServiceIds)] = ["Service IDs must be a collection of unique positive IDs."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }

        var booking = await repository.CreateAsync(
            request.RoomId,
            request.StartDateTime,
            request.EndDateTime,
            (room, hasOverlap) => CreateBooking(request, room, hasOverlap),
            cancellationToken);

        var services = booking.BookingServices
            .OrderBy(relation => relation.ServiceId)
            .Select(relation => new BookingServiceResponse(
                relation.ServiceId, relation.Service.Name, relation.PriceAtBooking))
            .ToArray();
        var servicesPrice = services.Sum(service => service.Price);
        return new BookingResponse(
            booking.Id,
            new BookingRoomResponse(booking.RoomId, booking.Room.Name),
            booking.StartDateTime,
            booking.EndDateTime,
            services,
            booking.TotalPrice - servicesPrice,
            servicesPrice,
            booking.TotalPrice);
    }

    private static Booking CreateBooking(CreateBookingRequest request, ConferenceRoom? room, bool hasOverlap)
    {
        if (room is null || !room.IsActive)
        {
            throw new EntityNotFoundException("The selected conference room was not found.");
        }

        var selectedServices = room.RoomServices
            .Where(relation => request.ServiceIds.Contains(relation.ServiceId) && relation.Service.IsActive)
            .Select(relation => relation.Service)
            .ToArray();
        if (selectedServices.Length != request.ServiceIds.Count)
        {
            throw new RequestValidationException(new()
            {
                [nameof(request.ServiceIds)] = ["Every selected service must be active and available for this room."]
            });
        }

        if (hasOverlap)
        {
            throw new BookingConflictException("The room is already booked during the selected period.");
        }

        var price = PricingCalculator.Calculate(
            room.BaseHourlyRate, request.StartDateTime, request.EndDateTime,
            selectedServices.Select(service => service.Price).ToArray());
        if (price.TotalPrice > MaximumTotalPrice)
        {
            throw new RequestValidationException(new()
            {
                [nameof(request.EndDateTime)] = ["The booking total exceeds the supported monetary amount."]
            });
        }

        var booking = new Booking
        {
            RoomId = room.Id,
            Room = room,
            StartDateTime = request.StartDateTime,
            EndDateTime = request.EndDateTime,
            TotalPrice = price.TotalPrice,
            CreatedAtUtc = DateTime.UtcNow
        };
        foreach (var service in selectedServices)
        {
            booking.BookingServices.Add(new BookingServiceEntity
            {
                Booking = booking,
                ServiceId = service.Id,
                Service = service,
                PriceAtBooking = service.Price
            });
        }

        return booking;
    }
}
