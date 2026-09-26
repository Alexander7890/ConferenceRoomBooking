using ConferenceRoomBooking.Application.DTOs.Rooms;
using ConferenceRoomBooking.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConferenceRoomBooking.Api.Controllers;

[ApiController]
[Route("api/rooms")]
public sealed class RoomsController(IConferenceRoomService roomService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConferenceRoomResponse>>> GetAll(CancellationToken cancellationToken)
        => Ok(await roomService.GetAllAsync(cancellationToken));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ConferenceRoomResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await roomService.GetByIdAsync(id, cancellationToken);
        return room is null ? NotFound() : Ok(room);
    }

    [HttpPost]
    public async Task<ActionResult<ConferenceRoomResponse>> Create(
        CreateConferenceRoomRequest request, CancellationToken cancellationToken)
    {
        var result = await roomService.CreateAsync(request, cancellationToken);
        return result.Room is { } room
            ? CreatedAtAction(nameof(GetById), new { id = room.Id }, room)
            : ValidationProblem(new ValidationProblemDetails(result.Errors));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ConferenceRoomResponse>> Update(
        int id, UpdateConferenceRoomRequest request, CancellationToken cancellationToken)
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
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
        => await roomService.DeleteAsync(id, cancellationToken) ? NoContent() : NotFound();
}
