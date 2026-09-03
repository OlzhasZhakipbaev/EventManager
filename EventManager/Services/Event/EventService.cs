using EventManager.DTOs;
using EventManager.Models;

namespace EventManager.Services.Event;

public class EventService : IEventService
{
    public List<EventModel> Events { get; set; } = [];
    
    public PaginatedResultDto<EventModel> GetEvents(EventRequestDto eventDto)
    {
        var page = Math.Max(eventDto.Page, 1);
        var pageSize = Math.Max(eventDto.PageSize, 1);
        var query = Events.AsEnumerable();
        
        if (!string.IsNullOrEmpty(eventDto.Title))
        {
            query = query.Where(x => x.Title.Contains(eventDto.Title, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (eventDto.From.HasValue)
        {
            query = query.Where(x => x.StartAt >= eventDto.From.Value).ToList();
        }
        
        if (eventDto.To.HasValue)
        {
            query = query.Where(x => x.EndAt <= eventDto.To.Value).ToList();
        }
        
        var totalCount = query.Count();
        
        var items = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();
        
        return new PaginatedResultDto<EventModel>
        {
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = items.Count,
            EventList = items
        };
    }

    public EventModel GetEvent(int id)
    {
        return Events.FirstOrDefault(x => x.Id == id);
    }
    
    public bool AddEvent(EventModel eventModel)
    {
        return CreateEventCore(eventModel) is not null;
    }

    public Task<EventModel?> CreateEventAsync(EventModel createEvent)
    {
        return Task.FromResult(CreateEventCore(createEvent));
    }

    private EventModel? CreateEventCore(EventModel createEvent)
    {
        if (Events.Any(x => x.Id == createEvent.Id))
            return null;

        var created = EventModel.Create(
            createEvent.Id,
            createEvent.Title,
            createEvent.Description,
            createEvent.StartAt,
            createEvent.EndAt,
            createEvent.TotalSeats);

        Events.Add(created);
        return created;
    }
    
    public bool ChangeEvent(int id, EventModel eventModel)
    {
        var _event = Events.FirstOrDefault(x => x.Id == id);

        if (_event is null)
        {
            return false;
        }
        
        _event.Title = eventModel.Title;
        _event.Description = eventModel.Description;
        _event.StartAt = eventModel.StartAt;
        _event.EndAt = eventModel.EndAt;
        
        return true;
    }
    
    public bool DeleteEvent(int id)
    {
        var findEvent = Events.FirstOrDefault(x => x.Id == id);
        if(findEvent is null)
        {
            return false;
        }
        
        Events.Remove(findEvent);

        return true;
    }
}