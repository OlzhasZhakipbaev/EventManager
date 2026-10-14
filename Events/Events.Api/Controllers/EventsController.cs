using System.ComponentModel.DataAnnotations;
using System.Net;
using Events.Api.Code;
using Events.Application.DTOs;
using Events.Application.Services;
using Events.Domain.Exceptions;
using Events.Domain.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Events.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    private readonly IEventService _eventService;

    public EventsController(IEventService eventService)
    {
        _eventService = eventService;
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
}
