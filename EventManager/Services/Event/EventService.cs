using EventManager.DataAccess;
using EventManager.DTOs;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Services.Event;

public class EventService : IEventService
{
    private readonly AppDbContext _context;

    public EventService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedResultDto<EventModel>> GetEventsAsync(EventRequestDto eventDto)
    {
        var page = Math.Max(eventDto.Page, 1);
        var pageSize = Math.Max(eventDto.PageSize, 1);
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(eventDto.Title))
        {
            var title = eventDto.Title.ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(title));
        }

        if (eventDto.From.HasValue)
            query = query.Where(x => x.StartAt >= eventDto.From.Value);

        if (eventDto.To.HasValue)
            query = query.Where(x => x.EndAt <= eventDto.To.Value);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResultDto<EventModel>
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = items.Count,
            EventList = items
        };
    }

    public Task<EventModel?> GetEventAsync(int id)
    {
        return _context.Events.FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<bool> AddEventAsync(EventModel eventModel)
    {
        if (await _context.Events.AnyAsync(x => x.Id == eventModel.Id))
            return false;

        var created = EventModel.Create(
            eventModel.Id,
            eventModel.Title,
            eventModel.Description,
            eventModel.StartAt,
            eventModel.EndAt,
            eventModel.TotalSeats);

        _context.Events.Add(created);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ChangeEventAsync(int id, EventModel eventModel)
    {
        var existing = await _context.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
            return false;

        existing.Title = eventModel.Title;
        existing.Description = eventModel.Description;
        existing.StartAt = eventModel.StartAt;
        existing.EndAt = eventModel.EndAt;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var existing = await _context.Events.FirstOrDefaultAsync(x => x.Id == id);
        if (existing is null)
            return false;

        _context.Events.Remove(existing);
        await _context.SaveChangesAsync();
        return true;
    }
}
