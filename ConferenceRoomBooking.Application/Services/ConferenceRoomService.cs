using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces;
using ConferenceRoomBooking.Application.Validation;
using ConferenceRoomBooking.Domain.Entities;

namespace ConferenceRoomBooking.Application.Services;

public sealed class ConferenceRoomService(IConferenceRoomRepository repository) : IConferenceRoomService
{
    public async Task<IReadOnlyList<ConferenceRoomResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rooms = await repository.GetAllAsync(cancellationToken);
        return rooms.Select(Map).ToArray();
    }

    public async Task<ConferenceRoomResponse?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var room = await repository.GetByIdAsync(id, cancellationToken);
        return room is null ? null : Map(room);
    }

    public async Task<ConferenceRoomWriteResult> CreateAsync(
        CreateConferenceRoomRequest request, CancellationToken cancellationToken)
    {
        var errors = ConferenceRoomRequestValidator.Validate(
            request.Name, request.Capacity, request.BaseHourlyRate, request.ServiceIds);
        if (errors.Count > 0)
        {
            return ConferenceRoomWriteResult.Invalid(errors);
        }

        var services = await repository.GetServicesByIdsAsync(request.ServiceIds, cancellationToken);
        if (!AreServicesAvailable(request.ServiceIds, services))
        {
            return InvalidServices();
        }

        var room = new ConferenceRoom
        {
            Name = request.Name.Trim(),
            Capacity = request.Capacity,
            BaseHourlyRate = request.BaseHourlyRate,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        UpdateServices(room, services);

        repository.Add(room);
        await repository.SaveChangesAsync(cancellationToken);
        return ConferenceRoomWriteResult.Success(Map(room));
    }

    public async Task<ConferenceRoomWriteResult> UpdateAsync(
        int id, UpdateConferenceRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await repository.GetForUpdateAsync(id, cancellationToken);
        if (room is null)
        {
            return ConferenceRoomWriteResult.NotFound();
        }

        var errors = ConferenceRoomRequestValidator.Validate(
            request.Name, request.Capacity, request.BaseHourlyRate, request.ServiceIds);
        if (errors.Count > 0)
        {
            return ConferenceRoomWriteResult.Invalid(errors);
        }

        var services = await repository.GetServicesByIdsAsync(request.ServiceIds, cancellationToken);
        if (!AreServicesAvailable(request.ServiceIds, services))
        {
            return InvalidServices();
        }

        room.Name = request.Name.Trim();
        room.Capacity = request.Capacity;
        room.BaseHourlyRate = request.BaseHourlyRate;
        room.UpdatedAtUtc = DateTime.UtcNow;
        UpdateServices(room, services);

        await repository.SaveChangesAsync(cancellationToken);
        return ConferenceRoomWriteResult.Success(Map(room));
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var room = await repository.GetForUpdateAsync(id, cancellationToken);
        if (room is null)
        {
            return false;
        }

        room.IsActive = false;
        room.UpdatedAtUtc = DateTime.UtcNow;
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static bool AreServicesAvailable(IReadOnlyCollection<int> requestedIds, IReadOnlyList<Service> services)
        => services.Count == requestedIds.Count && services.All(service => service.IsActive);

    private static ConferenceRoomWriteResult InvalidServices() => ConferenceRoomWriteResult.Invalid(new()
    {
        [nameof(CreateConferenceRoomRequest.ServiceIds)] = ["Every selected service must exist and be active."]
    });

    private static void UpdateServices(ConferenceRoom room, IReadOnlyList<Service> services)
    {
        var requestedIds = services.Select(service => service.Id).ToHashSet();
        var removedRelations = room.RoomServices
            .Where(roomService => !requestedIds.Contains(roomService.ServiceId))
            .ToArray();

        foreach (var relation in removedRelations)
        {
            room.RoomServices.Remove(relation);
        }

        var existingIds = room.RoomServices.Select(roomService => roomService.ServiceId).ToHashSet();
        foreach (var service in services.Where(service => !existingIds.Contains(service.Id)))
        {
            room.RoomServices.Add(new RoomService
            {
                RoomId = room.Id,
                Room = room,
                ServiceId = service.Id,
                Service = service
            });
        }
    }

    private static ConferenceRoomResponse Map(ConferenceRoom room) => new(
        room.Id,
        room.Name,
        room.Capacity,
        room.BaseHourlyRate,
        room.IsActive,
        room.RoomServices
            .Where(roomService => roomService.Service.IsActive)
            .OrderBy(roomService => roomService.ServiceId)
            .Select(roomService => new ConferenceRoomServiceResponse(
                roomService.Service.Id, roomService.Service.Name, roomService.Service.Price))
            .ToArray());
}
