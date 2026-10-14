using Contracts;
using Events.Api.Security;
using Events.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Events.Api.Controllers;

[ApiController]
[Route("internal/events")]
[InternalApiKey]
public class InternalEventsController : ControllerBase
{
    private readonly IEventService _events;

    public InternalEventsController(IEventService events)
    {
        _events = events;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<EventInfoDto>> Get(int id)
    {
        var ev = await _events.GetEventAsync(id);
        if (ev is null)
            return NotFound();

        return new EventInfoDto
        {
            Id = ev.Id,
            StartAt = ev.StartAt
        };
    }

    [HttpPost("{id:int}/seats/reserve")]
    public async Task<IActionResult> Reserve(int id)
    {
        var ev = await _events.GetEventAsync(id);
        if (ev is null)
            return NotFound();

        if (!await _events.ReserveSeatAsync(id))
        {
            return Conflict(new SeatReservationResult
            {
                Success = false,
                AvailableSeats = ev.AvailableSeats
            });
        }

        return NoContent();
    }

    [HttpPost("{id:int}/seats/release")]
    public async Task<IActionResult> Release(int id)
    {
        await _events.ReleaseSeatAsync(id);
        return NoContent();
    }
}
