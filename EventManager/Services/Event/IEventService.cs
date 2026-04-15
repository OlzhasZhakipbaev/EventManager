using EventManager.Models;

namespace EventManager.Services.Event;

public interface IEventService
{
    List<EventModel> GetEvents();
    EventModel GetEvent(int id);
    bool AddEvent(EventModel eventModel);
    bool ChangeEvent(int id, EventModel eventModel);
    bool DeleteEvent(int id);
}