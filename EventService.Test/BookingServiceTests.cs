using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Models.Enums;
using EventManager.Services.Booking;
using eventService = EventManager.Services.Event.EventService;

namespace EventService.Test;

public class BookingServiceTests
{
    private static (BookingService Booking, eventService Events) CreateServices()
    {
        var events = new eventService();
        events.Events.Add(new EventModel
        {
            Id = 1,
            Title = "Conference",
            Description = "Desc",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2)
        });

        return (new BookingService(events), events);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_Pending_For_Existing_Event()
    {
        var (booking, _) = CreateServices();

        var result = await booking.CreateBookingAsync(1);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Multiple_Bookings_With_Unique_Ids()
    {
        var (booking, _) = CreateServices();

        var first = await booking.CreateBookingAsync(1);
        var second = await booking.CreateBookingAsync(1);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(1, first.EventId);
        Assert.Equal(1, second.EventId);
        Assert.Equal(BookingStatus.Pending, first.Status);
        Assert.Equal(BookingStatus.Pending, second.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Booking()
    {
        var (booking, _) = CreateServices();
        var created = await booking.CreateBookingAsync(1);

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Confirmed_Status()
    {
        var (booking, _) = CreateServices();
        var created = await booking.CreateBookingAsync(1);

        await booking.ProcessPendingAsync(CancellationToken.None);

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Rejected_Status()
    {
        var (booking, _) = CreateServices();
        var created = await booking.CreateBookingAsync(1);

        created.Status = BookingStatus.Rejected;

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Event_Not_Found()
    {
        var (booking, _) = CreateServices();

        await Assert.ThrowsAsync<NotFoundException>(() => booking.CreateBookingAsync(999));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Event_Deleted()
    {
        var (booking, events) = CreateServices();
        events.DeleteEvent(1);

        await Assert.ThrowsAsync<NotFoundException>(() => booking.CreateBookingAsync(1));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Null_When_Not_Found()
    {
        var (booking, _) = CreateServices();

        var result = await booking.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }
}
