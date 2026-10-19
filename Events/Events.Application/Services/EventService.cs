using System.Text.Json;
using Events.Application.Caching;
using Events.Application.DTOs;
using Events.Application.Repositories;
using Events.Domain.Models;

namespace Events.Application.Services;

public sealed record CacheTtlOptions(TimeSpan EventTtl, TimeSpan TopTtl);

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly ICache _cache;
    private readonly TimeSpan _eventTtl;
    private readonly TimeSpan _topTtl;

    public EventService(IEventRepository events, ICache cache, CacheTtlOptions ttl)
    {
        _events = events;
        _cache = cache;
        _eventTtl = ttl.EventTtl;
        _topTtl = ttl.TopTtl;
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

    public async Task<EventModel?> GetEventAsync(int id)
    {
        var key = CacheKeys.Event(id);
        var cached = await _cache.GetAsync(key);
        if (!string.IsNullOrEmpty(cached))
        {
            var fromCache = JsonSerializer.Deserialize<EventModel>(cached);
            if (fromCache is not null)
                return fromCache;
        }

        var ev = await _events.GetByIdAsync(id);
        if (ev is null)
            return null;

        await _cache.SetAsync(key, JsonSerializer.Serialize(ev), _eventTtl);
        return ev;
    }

    public async Task<IReadOnlyList<EventModel>> GetTopEventsAsync()
    {
        const string key = CacheKeys.TopEvents;
        var cached = await _cache.GetAsync(key);
        if (!string.IsNullOrEmpty(cached))
        {
            var fromCache = JsonSerializer.Deserialize<List<EventModel>>(cached);
            if (fromCache is not null)
                return fromCache;
        }

        var items = await _events.GetTopBySoldPercentAsync(10);
        await _cache.SetAsync(key, JsonSerializer.Serialize(items), _topTtl);
        return items;
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
        await _cache.RemoveAsync(CacheKeys.Event(id));
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var existing = await _events.GetByIdAsync(id);
        if (existing is null)
            return false;

        _events.Remove(existing);
        await _events.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.Event(id));
        return true;
    }

    public async Task<bool> ReserveSeatAsync(int id)
    {
        var existing = await _events.GetByIdAsync(id);
        if (existing is null || !existing.TryReserveSeats())
            return false;

        await _events.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.Event(id));
        return true;
    }

    public async Task ReleaseSeatAsync(int id)
    {
        var existing = await _events.GetByIdAsync(id);
        if (existing is null)
            return;

        existing.ReleaseSeats();
        await _events.SaveChangesAsync();
        await _cache.RemoveAsync(CacheKeys.Event(id));
    }
}
