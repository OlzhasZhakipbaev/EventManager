using EventManager.DTOs;
using EventManager.Models;
using EventManager.Repositories;

namespace EventManager.Services.Event;

public class EventService : IEventService
{
    private readonly IEventRepository _events;

    public EventService(IEventRepository events)
    {
        _events = events;
    }

    public async Task<PaginatedResultDto<EventModel>> GetEventsAsync(EventRequestDto eventDto)
    {
        var page = Math.Max(eventDto.Page, 1);
        var pageSize = Math.Max(eventDto.PageSize, 1);
        var (items, totalCount) = await _events.GetPagedAsync(
            eventDto.Title,
            eventDto.From,
            eventDto.To,
            (page - 1) * pageSize,
            pageSize);

        return new PaginatedResultDto<EventModel>
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = items.Count,
            EventList = items.ToList()
        };
    }

    public Task<EventModel?> GetEventAsync(int id)
    {
        return _events.GetByIdAsync(id);
    }

    public async Task<bool> AddEventAsync(EventModel eventModel)
    {
        if (await _events.ExistsAsync(eventModel.Id))
            return false;

        var created = EventModel.Create(
            eventModel.Id,
            eventModel.Title,
            eventModel.Description,
            eventModel.StartAt,
            eventModel.EndAt,
            eventModel.TotalSeats);

        await _events.AddAsync(created);
        await _events.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeEventAsync(int id, EventModel eventModel)
    {
        var existing = await _events.GetByIdAsync(id);
        if (existing is null)
            return false;

        existing.Title = eventModel.Title;
        existing.Description = eventModel.Description;
        existing.StartAt = eventModel.StartAt;
        existing.EndAt = eventModel.EndAt;

        await _events.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var existing = await _events.GetByIdAsync(id);
        if (existing is null)
            return false;

        _events.Remove(existing);
        await _events.SaveChangesAsync();
        return true;
    }
}
