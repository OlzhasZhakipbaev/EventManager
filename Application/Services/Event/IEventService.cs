using Application.DTOs;
using Domain.Models;

namespace Application.Services.Event;

public interface IEventService
{
    Task<PaginatedResultDto<EventModel>> GetEventsAsync(EventRequestDto request);
    Task<EventModel?> GetEventAsync(int id);
    Task<bool> AddEventAsync(EventModel eventModel);
    Task<bool> ChangeEventAsync(int id, EventModel eventModel);
    Task<bool> DeleteEventAsync(int id);
}
