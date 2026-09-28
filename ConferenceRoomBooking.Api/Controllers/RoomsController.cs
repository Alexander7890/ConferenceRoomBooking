using System.ComponentModel;
using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError, "application/problem+json",
    Description = "An unexpected server error occurred.")]
public sealed class RoomsController(
    IConferenceRoomService roomService, IAvailabilityService availabilityService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List active conference rooms")]
    [EndpointDescription("Returns active rooms with their capacity, base hourly rate and available active services.")]
    [ProducesResponseType<IReadOnlyList<ConferenceRoomResponse>>(StatusCodes.Status200OK, "application/json",
        Description = "The active conference rooms, or an empty list.")]
    public async Task<ActionResult<IReadOnlyList<ConferenceRoomResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await roomService.GetAllAsync(cancellationToken));

    [HttpGet("available")]
    [EndpointSummary("Find available conference rooms")]
    [EndpointDescription("Returns active rooms that meet the requested capacity and have no overlapping booking. " +
        "Use local times without a UTC suffix or offset, within one calendar day from 06:00 to 23:00. " +
        "A booking ending exactly when another begins does not overlap.")]
    [ProducesResponseType<IReadOnlyList<ConferenceRoomResponse>>(StatusCodes.Status200OK, "application/json",
        Description = "The matching rooms, or an empty list when none are available.")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
        Description = "The time range or requested capacity is invalid.")]
    public async Task<ActionResult<IReadOnlyList<ConferenceRoomResponse>>> GetAvailable(
        [FromQuery] RoomAvailabilityRequest request, CancellationToken cancellationToken)
        => Ok(await availabilityService.FindAsync(request, cancellationToken));

    [HttpGet("{id:int}")]
    [EndpointSummary("Get a conference room")]
    [EndpointDescription("Returns an active room and its available active services. Soft-deleted rooms are not returned.")]
    [ProducesResponseType<ConferenceRoomResponse>(StatusCodes.Status200OK, "application/json",
        Description = "The requested active conference room.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json",
        Description = "The room does not exist or is inactive.")]
    public async Task<ActionResult<ConferenceRoomResponse>> GetById(
        [Description("The conference room ID.")] int id, CancellationToken cancellationToken)
    {
        var room = await roomService.GetByIdAsync(id, cancellationToken);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPost]
    [EndpointSummary("Create a conference room")]
    [EndpointDescription("Creates an active room and assigns the selected existing active services. " +
        "Service IDs must be unique; an empty list creates a room without additional services. " +
        "The response includes the new room ID and a Location header for its resource URL.")]
    [ProducesResponseType<ConferenceRoomResponse>(StatusCodes.Status201Created, "application/json",
        Description = "The conference room was created.")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
        Description = "Room fields or selected service IDs are invalid.")]
    public async Task<ActionResult<ConferenceRoomResponse>> Create(
        CreateConferenceRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.CreateAsync(request, cancellationToken);
        return result.Room is { } room
            ? CreatedAtAction(nameof(GetById), new { id = room.Id }, room)
            : ValidationProblem(new ValidationProblemDetails(result.Errors));
    }

    [HttpPut("{id:int}")]
    [EndpointSummary("Update a conference room")]
    [EndpointDescription("Updates an active room and replaces its available service selection atomically. " +
        "Removing a service from the selection does not delete the service itself.")]
    [ProducesResponseType<ConferenceRoomResponse>(StatusCodes.Status200OK, "application/json",
        Description = "The updated conference room.")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json",
        Description = "Room fields or selected service IDs are invalid.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json",
        Description = "The room does not exist or is inactive.")]
    public async Task<ActionResult<ConferenceRoomResponse>> Update(
        [Description("The conference room ID.")] int id,
        UpdateConferenceRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.UpdateAsync(id, request, cancellationToken);
        if (result.IsNotFound)
        {
            return NotFound();
        }

        return result.Room is { } room
            ? Ok(room)
            : ValidationProblem(new ValidationProblemDetails(result.Errors));
    }

    [HttpDelete("{id:int}")]
    [EndpointSummary("Soft-delete a conference room")]
    [EndpointDescription("Marks the room inactive and preserves its historical bookings and service relations. " +
        "Deleting a missing or already inactive room returns 404.")]
    [ProducesResponseType(StatusCodes.Status204NoContent, Description = "The conference room was marked inactive.")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json",
        Description = "The room does not exist or is already inactive.")]
    public async Task<IActionResult> Delete(
        [Description("The conference room ID.")] int id, CancellationToken cancellationToken)
        => await roomService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
