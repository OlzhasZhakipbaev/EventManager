using EventManager.DataAccess;
using EventManager.DTOs;
using EventManager.Models;
using EventManager.Services.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Test;

public class EventServiceTests
{
    private static IServiceProvider CreateProvider()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventService, EventManager.Services.Event.EventService>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Events.AddRange(
            EventModel.Create(1, "Conference", "Desc1", new DateTime(2026, 1, 10), new DateTime(2026, 1, 11), 10),
            EventModel.Create(2, "Meeting", "Desc2", new DateTime(2026, 2, 10), new DateTime(2026, 2, 11), 10),
            EventModel.Create(3, "Workshop", "Desc3", new DateTime(2026, 3, 10), new DateTime(2026, 3, 11), 10));
        context.SaveChanges();

        return provider;
    }

    private static IEventService CreateService(IServiceProvider provider)
    {
        return provider.CreateScope().ServiceProvider.GetRequiredService<IEventService>();
    }

    [Fact]
    public async Task AddEvent_Should_Add_Event()
    {
        var provider = CreateProvider();
        var service = CreateService(provider);

        var model = EventModel.Create(10, "New Event", "Description", DateTime.Today, DateTime.Today.AddDays(1), 20);

        var result = await service.AddEventAsync(model);

        Assert.True(result);

        var all = await service.GetEventsAsync(new EventRequestDto { Page = 1, PageSize = 10 });
        Assert.Equal(4, all.TotalCount);
        var added = await service.GetEventAsync(10);
        Assert.Equal(20, added!.TotalSeats);
        Assert.Equal(20, added.AvailableSeats);
    }

    [Fact]
    public async Task GetEvents_Should_Return_All()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventsAsync(new EventRequestDto
        {
            Page = 1,
            PageSize = 10
        });

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(3, result.EventList.Count);
    }

    [Fact]
    public async Task GetEvent_Should_Return_Event()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventAsync(2);

        Assert.NotNull(result);
        Assert.Equal("Meeting", result.Title);
    }

    [Fact]
    public async Task ChangeEvent_Should_Update_Event()
    {
        var service = CreateService(CreateProvider());

        var model = EventModel.Create(1, "Updated", "Updated Description", DateTime.Today, DateTime.Today.AddDays(2), 10);

        var result = await service.ChangeEventAsync(1, model);

        Assert.True(result);

        Assert.Equal("Updated", (await service.GetEventAsync(1))!.Title);
    }

    [Fact]
    public async Task DeleteEvent_Should_Remove_Event()
    {
        var service = CreateService(CreateProvider());

        var result = await service.DeleteEventAsync(1);

        Assert.True(result);
        var all = await service.GetEventsAsync(new EventRequestDto { Page = 1, PageSize = 10 });
        Assert.Equal(2, all.TotalCount);
    }

    [Fact]
    public async Task GetEvents_Should_Filter_By_Title()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventsAsync(new EventRequestDto
        {
            Title = "Meet",
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }

    [Fact]
    public async Task GetEvents_Should_Filter_By_Dates()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventsAsync(new EventRequestDto
        {
            From = new DateTime(2026, 2, 1),
            To = new DateTime(2026, 2, 28),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }

    [Fact]
    public async Task GetEvents_Should_Return_Second_Page()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventsAsync(new EventRequestDto
        {
            Page = 2,
            PageSize = 2
        });

        Assert.Single(result.EventList);
        Assert.Equal("Workshop", result.EventList.First().Title);
    }

    [Fact]
    public async Task GetEvents_Should_Filter_By_Title_And_Date()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventsAsync(new EventRequestDto
        {
            Title = "Meet",
            From = new DateTime(2026, 2, 1),
            To = new DateTime(2026, 2, 28),
            Page = 1,
            PageSize = 10
        });

        Assert.Single(result.EventList);
        Assert.Equal("Meeting", result.EventList.First().Title);
    }

    [Fact]
    public async Task GetEvent_Should_Return_Null_When_Not_Found()
    {
        var service = CreateService(CreateProvider());

        var result = await service.GetEventAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task ChangeEvent_Should_Return_False_When_Not_Found()
    {
        var service = CreateService(CreateProvider());

        var result = await service.ChangeEventAsync(999, EventModel.Create(999, "x", null, DateTime.Today, DateTime.Today.AddDays(1), 1));

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteEvent_Should_Return_False_When_Not_Found()
    {
        var service = CreateService(CreateProvider());

        var result = await service.DeleteEventAsync(999);

        Assert.False(result);
    }
}
