using System.ComponentModel.DataAnnotations;
using System.Net;
using EventManager.Code;
using EventManager.DTOs;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Services.Booking;
using EventManager.Services.Event;
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
    public ApiResult<PaginatedResultDto<EventModel>> GetAll([FromQuery] EventRequestDto eventRequest)
    {
        return new ApiResult<PaginatedResultDto<EventModel>>
        {
            Success = true,
            StatusCode = System.Net.HttpStatusCode.OK,
            Message = "События успешно получены",
            Data = _eventService.GetEvents(eventRequest)
        };
    }
    
    [HttpGet("{id:int}")]
    public ApiResult<EventModel> GetEvent([FromRoute] int id)
    {
        var result = _eventService.GetEvent(id);

        if (result is null)
        {
            throw new NotFoundException("Не удалось найти событие");
        }
        {
            return new ApiResult<EventModel>
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие успешно получено",
                Data = result
            };
        }
    }
    
    [HttpPost]
    public ApiResult<bool> AddEvent([FromBody] EventModel eventModel)
    {
            var result = _eventService.AddEvent(eventModel);
            if (result)
            {
                return new ApiResult<bool>()
                {
                    Success = true,
                    StatusCode = HttpStatusCode.Created,
                    Message = "Событие успешно добавлено",
                    Data = result
                };
            }

            {
                throw new ValidationException();
            }
    }
    
    [HttpPut("{id:int}")]
    public ApiResult<bool> ChangeEvent([FromRoute] int id, [FromBody] ChangeEventDto eventModelDto)
    {
        var eventModel = new EventModel()
        {
            Title = eventModelDto.Title,
            Description = eventModelDto.Description,
            StartAt = eventModelDto.StartAt,
            EndAt = eventModelDto.EndAt
        };
        
        var result = _eventService.ChangeEvent(id , eventModel);
        if (result)
        {
            return new ApiResult<bool>()
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие изменено",
                Data =  result
            };
        }
        {
            throw new NotFoundException("Событие не найдено");
        }
    }
    
    [HttpDelete("{id:int}")]
    public ApiResult<bool> DeleteEvent([FromRoute] int id)
    {
        var result = _eventService.DeleteEvent(id);
        if (result)
        {
            return new ApiResult<bool>()
            {
                Success = true,
                StatusCode = HttpStatusCode.OK,
                Message = "Событие удалено",
                Data =  result
            };
        }
        {
            throw new NotFoundException("Событие не найдено");
        }
    }
    
    [HttpPost("{eventId:int}/book")]
    public async Task<ApiResult<BookingModel>> BookEvent([FromRoute] int eventId)
    {
        var result = await _bookingService.CreateBookingAsync(eventId);
        
        Response.Headers.Location = $"/bookings/{result.Id}";
        
        return new ApiResult<BookingModel>()
        {
            Success = true,
            StatusCode = HttpStatusCode.Accepted,
            Message = $"Номер брони {result.Id}. Событие c id {result.EventId} забронировано со статусом {result.Status}",
            Data =  result
        };
    }
}