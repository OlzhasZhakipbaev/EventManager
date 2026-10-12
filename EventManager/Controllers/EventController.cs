using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using Application.DTOs;
using EventManager.Code;
using Domain.Exceptions;
using Domain.Models;
using Application.Services.Booking;
using Application.Services.Event;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EventManager.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;
    private readonly IBookingService _bookingService;

    public EventsController(IEventService eventService, IBookingService bookingService)
    {
        _eventService = eventService;
        _bookingService = bookingService;
    }

    [HttpGet]
    public async Task<ApiResult<PaginatedResultDto<EventModel>>> GetAll([FromQuery] EventRequestDto eventRequest)
    {
        return new ApiResult<PaginatedResultDto<EventModel>>
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "События успешно получены",
            Data = await _eventService.GetEventsAsync(eventRequest)
        };
    }

    [HttpGet("{id:int}")]
    public async Task<ApiResult<EventModel>> GetEvent([FromRoute] int id)
    {
        var result = await _eventService.GetEventAsync(id);

        if (result is null)
            throw new NotFoundException("Не удалось найти событие");

        return new ApiResult<EventModel>
        {
            Success = true,
            StatusCode = HttpStatusCode.OK,
            Message = "Событие успешно получено",
            Data = result
        };
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult<bool>> AddEvent([FromBody] EventModel eventModel)
    {
        var result = await _eventService.AddEventAsync(eventModel);
        if (result)
        {
            return new ApiResult<bool>
            {
                Success = true,
                StatusCode = HttpStatusCode.Created,
                Message = "Событие успешно добавлено",
                Data = result
            };
        }

        throw new ValidationException();
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult<bool>> ChangeEvent([FromRoute] int id, [FromBody] ChangeEventDto eventModelDto)
    {
        var eventModel = EventModel.Create(
            id,
            eventModelDto.Title,
            eventModelDto.Description,
            eventModelDto.StartAt,
            eventModelDto.EndAt,
            1);

        var result = await _eventService.ChangeEventAsync(id, eventModel);
        if (result)
        {
            return new ApiResult<bool>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие изменено",
                Data = result
            };
        }

        throw new NotFoundException("Событие не найдено");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult<bool>> DeleteEvent([FromRoute] int id)
    {
        var result = await _eventService.DeleteEventAsync(id);
        if (result)
        {
            return new ApiResult<bool>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие удалено",
                Data = result
            };
        }

        throw new NotFoundException("Событие не найдено");
    }

    [HttpPost("{eventId:int}/book")]
    [Authorize]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ApiResult<BookingModel>> BookEvent([FromRoute] int eventId)
    {
        var result = await _bookingService.CreateBookingAsync(eventId, ResolveUserId());

        Response.Headers.Location = $"/bookings/{result.Id}";

        return new ApiResult<BookingModel>
        {
            Success = true,
            StatusCode = HttpStatusCode.Accepted,
            Message = $"Номер брони {result.Id}. Событие c id {result.EventId} забронировано со статусом {result.Status}",
            Data = result
        };
    }

    private Guid ResolveUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? User.FindFirstValue("sub");
        if (!Guid.TryParse(raw, out var id))
            throw new AccessDeniedException(Guid.Empty, "book");
        return id;
    }
}
