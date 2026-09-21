using EventManager.DTOs;
using EventManager.Models;
using EventManager.Services.Event;
using Moq;
using eventService = EventManager.Services.Event.EventService;

namespace EventService.Test;

public class EventServiceTests
{
    private eventService CreateService()
    {
        var service = new eventService();

        service.Events.AddRange(new[]
        {
            new EventModel
            {
                Id = 1,
                Title = "Conference",
                Description = "Desc1",
                StartAt = new DateTime(2026, 1, 10),
                EndAt = new DateTime(2026, 1, 11),
                TotalSeats = 10,
                AvailableSeats = 10
            },
            new EventModel
            {
                Id = 2,
                Title = "Meeting",
                Description = "Desc2",
                StartAt = new DateTime(2026, 2, 10),
                EndAt = new DateTime(2026, 2, 11),
                TotalSeats = 10,
                AvailableSeats = 10
            },
            new EventModel
            {
                Id = 3,
                Title = "Workshop",
                Description = "Desc3",
                StartAt = new DateTime(2026, 3, 10),
                EndAt = new DateTime(2026, 3, 11),
                TotalSeats = 10,
                AvailableSeats = 10
            }
        });

        return service;
    }

    [Fact]
    public void AddEvent_Should_Add_Event()
    {
        var service = CreateService();

        var model = new EventModel
        {
            Id = 10,
            Title = "New Event",
            Description = "Description",
            StartAt = DateTime.Today,
            EndAt = DateTime.Today.AddDays(1),
            TotalSeats = 20
        };

        var result = service.AddEvent(model);

        Assert.True(result);

        Assert.Equal(4, service.Events.Count);
        var added = service.GetEvent(10);
        Assert.Equal(20, added.TotalSeats);
        Assert.Equal(20, added.AvailableSeats);
    }
    
    [Fact]
    public void GetEvents_Should_Return_All()
    {
        var service = CreateService();

        var result = service.GetEvents(new EventRequestDto
        {
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.EventList.Count);
    }
    
    [Fact]
    public void GetEvent_Should_Return_Event()
    {
        var service = CreateService();

        var result = service.GetEvent(2);

        Assert.NotNull(result);
        Assert.Equal("Meeting", result.Title);
    }
    
    [Fact]
    public void ChangeEvent_Should_Update_Event()
    {
        var service = CreateService();

        var model = new EventModel
        {
            Title = "Updated",
            Description = "Updated Description",
            StartAt = DateTime.Today,
            EndAt = DateTime.Today.AddDays(2)
        };

        var result = service.ChangeEvent(1, model);

        Assert.True(result);

        Assert.Equal("Updated", service.GetEvent(1).Title);
    }
    
    [Fact]
    public void DeleteEvent_Should_Remove_Event()
    {
        var service = CreateService();

        var result = service.DeleteEvent(1);

        Assert.True(result);
        Assert.Equal(2, service.Events.Count);
    }
    
    [Fact]
    public void GetEvents_Should_Filter_By_Title()
    {
        var service = CreateService();

        var result = service.GetEvents(new EventRequestDto
        {
            Title = "Meet",
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }
    
    [Fact]
    public void GetEvents_Should_Filter_By_Dates()
    {
        var service = CreateService();

        var result = service.GetEvents(new EventRequestDto
        {
            From = new DateTime(2026,2,1),
            To = new DateTime(2026,2,28),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }
    
    [Fact]
    public void GetEvents_Should_Return_Second_Page()
    {
        var service = CreateService();

        var result = service.GetEvents(new EventRequestDto
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Single(result.EventList);
        Assert.Equal("Workshop", result.EventList.First().Title);
    }
    
    [Fact]
    public void GetEvents_Should_Filter_By_Title_And_Date()
    {
        var service = CreateService();

        var result = service.GetEvents(new EventRequestDto
        {
            Title = "Meet",
            From = new DateTime(2026,2,1),
            To = new DateTime(2026,2,28),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }
    
    [Fact]
    public void GetEvent_Should_Return_Null_When_Not_Found()
    {
        var service = CreateService();

        var result = service.GetEvent(999);

        Assert.Null(result);
    }
    
    [Fact]
    public void ChangeEvent_Should_Return_False_When_Not_Found()
    {
        var service = CreateService();

        var result = service.ChangeEvent(999, new EventModel());

        Assert.False(result);
    }
    
    [Fact]
    public void DeleteEvent_Should_Return_False_When_Not_Found()
    {
        var service = CreateService();

        var result = service.DeleteEvent(999);

        Assert.False(result);
    }
}