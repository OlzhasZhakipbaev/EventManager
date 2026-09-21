using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Models.Enums;
using EventManager.Services.Booking;
using eventService = EventManager.Services.Event.EventService;

namespace EventService.Test;

public class BookingServiceTests
{
    private static EventModel CreateTestEvent(int totalSeats, int id = 1)
    {
        return new EventModel
        {
            Id = id,
            Title = "Conference",
            Description = "Desc",
            StartAt = DateTime.UtcNow.AddDays(1),
            EndAt = DateTime.UtcNow.AddDays(2),
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    private static (BookingService Booking, eventService Events) CreateServices(int totalSeats = 10)
    {
        var events = new eventService();
        events.Events.Add(CreateTestEvent(totalSeats));

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
    public async Task CreateBookingAsync_Should_Decrease_AvailableSeats_By_One()
    {
        var (booking, events) = CreateServices(totalSeats: 5);

        await booking.CreateBookingAsync(1);

        Assert.Equal(4, events.GetEvent(1).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Multiple_Bookings_Up_To_Limit_With_Unique_Ids()
    {
        var (booking, events) = CreateServices(totalSeats: 3);

        var first = await booking.CreateBookingAsync(1);
        var second = await booking.CreateBookingAsync(1);
        var third = await booking.CreateBookingAsync(1);

        Assert.Equal(3, new[] { first.Id, second.Id, third.Id }.Distinct().Count());
        Assert.Equal(0, events.GetEvent(1).AvailableSeats);
        Assert.Equal(BookingStatus.Pending, first.Status);
        Assert.Equal(BookingStatus.Pending, second.Status);
        Assert.Equal(BookingStatus.Pending, third.Status);
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
    public async Task CreateBookingAsync_Should_Throw_When_Seats_Exhausted()
    {
        var (booking, _) = CreateServices(totalSeats: 1);

        await booking.CreateBookingAsync(1);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => booking.CreateBookingAsync(1));
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
    public async Task Confirm_Should_Set_Confirmed_Status_And_ProcessedAt()
    {
        var (booking, _) = CreateServices();
        var created = await booking.CreateBookingAsync(1);

        created.Confirm();

        Assert.Equal(BookingStatus.Confirmed, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_Should_Set_Rejected_Status_And_ProcessedAt()
    {
        var (booking, _) = CreateServices();
        var created = await booking.CreateBookingAsync(1);

        created.Reject();

        Assert.Equal(BookingStatus.Rejected, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_Then_ReleaseSeats_Should_Restore_AvailableSeats()
    {
        var (booking, events) = CreateServices(totalSeats: 3);
        var created = await booking.CreateBookingAsync(1);
        var ev = events.GetEvent(1);

        created.Reject();
        ev.ReleaseSeats();

        Assert.Equal(3, ev.AvailableSeats);
    }

    [Fact]
    public async Task Reject_Then_ReleaseSeats_Should_Allow_New_Booking()
    {
        var (booking, events) = CreateServices(totalSeats: 1);
        var created = await booking.CreateBookingAsync(1);

        created.Reject();
        events.GetEvent(1).ReleaseSeats();

        var next = await booking.CreateBookingAsync(1);

        Assert.NotEqual(created.Id, next.Id);
        Assert.Equal(BookingStatus.Pending, next.Status);
        Assert.Equal(0, events.GetEvent(1).AvailableSeats);
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
    public async Task CreateBookingAsync_Should_Throw_When_No_Available_Seats()
    {
        var (booking, events) = CreateServices(totalSeats: 1);
        events.GetEvent(1).AvailableSeats = 0;

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => booking.CreateBookingAsync(1));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Null_When_Not_Found()
    {
        var (booking, _) = CreateServices();

        var result = await booking.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Prevent_Overbooking_Under_Concurrency()
    {
        var (booking, events) = CreateServices(totalSeats: 5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                try
                {
                    return await booking.CreateBookingAsync(1);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);
        var succeeded = results.Where(x => x is not null).ToList();

        Assert.Equal(5, succeeded.Count);
        Assert.Equal(15, results.Count(x => x is null));
        Assert.Equal(0, events.GetEvent(1).AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Assign_Unique_Ids_Under_Concurrency()
    {
        var (booking, _) = CreateServices(totalSeats: 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(() => booking.CreateBookingAsync(1)));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);
        Assert.Equal(10, results.Select(x => x.Id).Distinct().Count());
    }
}
