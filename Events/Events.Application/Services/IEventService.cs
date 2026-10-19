using Events.Application.DTOs;
using Events.Domain.Models;

namespace Events.Application.Services;

public interface IEventService
{
    Task<PaginatedResultDto<EventModel>> GetEventsAsync(EventRequestDto request);
    Task<EventModel?> GetEventAsync(int id);
    Task<IReadOnlyList<EventModel>> GetTopEventsAsync();
    Task<bool> AddEventAsync(EventModel eventModel);
    Task<bool> ChangeEventAsync(int id, EventModel eventModel);
    Task<bool> DeleteEventAsync(int id);
    Task<bool> ReserveSeatAsync(int id);
    Task ReleaseSeatAsync(int id);
}
