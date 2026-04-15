using EventManager.Models;

namespace EventManager.Services.Event;

public class EventService : IEventService
{
    public List<EventModel> Events { get; set; } = [];
    
    public List<EventModel> GetEvents()
    {
        return Events;
    }

    public EventModel GetEvent(int id)
    {
        return Events.FirstOrDefault(x => x.Id == id);
    }
    
    public bool AddEvent(EventModel eventModel)
    {
        var _event = Events.FirstOrDefault(x => x.Id == eventModel.Id);
        
        if(_event is not null)
        {
            return false;
        }
        
        Events.Add(eventModel);

        return true;
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
        _event.Id = eventModel.Id;
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