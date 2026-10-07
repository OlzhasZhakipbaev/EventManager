using Application.Repositories;
using Infrastructure.DataAccess;
using Domain.Exceptions;
using Domain.Models;
using Domain.Models.Enums;
using Infrastructure.Repositories;
using Application.Services.Booking;
using Application.Services.Event;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EventService.Test;

public class BookingServiceTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static EventModel CreateTestEvent(int totalSeats, int id = 1, DateTime? startAt = null)
    {
        var start = startAt ?? DateTime.UtcNow.AddDays(1);
        return EventModel.Create(
            id,
            "Conference",
            "Desc",
            start,
            start.AddDays(1),
            totalSeats);
    }

    private static IServiceProvider CreateProvider(int totalSeats = 10, DateTime? startAt = null)
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(dbName));
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventService, Application.Services.Event.EventService>();
        services.AddScoped<IBookingService, BookingService>();

        var provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Events.Add(CreateTestEvent(totalSeats, startAt: startAt));
        context.SaveChanges();

        return provider;
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Return_Pending_For_Existing_Event()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var result = await booking.CreateBookingAsync(1, UserId);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(1, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
        Assert.Null(result.ProcessedAt);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Decrease_AvailableSeats_By_One()
    {
        using var scope = CreateProvider(totalSeats: 5).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();

        await booking.CreateBookingAsync(1, UserId);

        Assert.Equal(4, (await events.GetEventAsync(1))!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Multiple_Bookings_Up_To_Limit_With_Unique_Ids()
    {
        using var scope = CreateProvider(totalSeats: 3).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();

        var first = await booking.CreateBookingAsync(1, UserId);
        var second = await booking.CreateBookingAsync(1, UserId);
        var third = await booking.CreateBookingAsync(1, UserId);

        Assert.Equal(3, new[] { first.Id, second.Id, third.Id }.Distinct().Count());
        Assert.Equal(0, (await events.GetEventAsync(1))!.AvailableSeats);
        Assert.Equal(BookingStatus.Pending, first.Status);
        Assert.Equal(BookingStatus.Pending, second.Status);
        Assert.Equal(BookingStatus.Pending, third.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Create_Multiple_Bookings_With_Unique_Ids()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var first = await booking.CreateBookingAsync(1, UserId);
        var second = await booking.CreateBookingAsync(1, UserId);

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal(1, first.EventId);
        Assert.Equal(1, second.EventId);
        Assert.Equal(BookingStatus.Pending, first.Status);
        Assert.Equal(BookingStatus.Pending, second.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Seats_Exhausted()
    {
        using var scope = CreateProvider(totalSeats: 1).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await booking.CreateBookingAsync(1, UserId);

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => booking.CreateBookingAsync(1, UserId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Booking()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(created.Id, result.Id);
        Assert.Equal(created.EventId, result.EventId);
        Assert.Equal(BookingStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Confirmed_Status()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        await booking.ProcessPendingAsync(CancellationToken.None);

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Confirmed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task Confirm_Should_Set_Confirmed_Status_And_ProcessedAt()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        created.Confirm();

        Assert.Equal(BookingStatus.Confirmed, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_Should_Set_Rejected_Status_And_ProcessedAt()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        created.Reject();

        Assert.Equal(BookingStatus.Rejected, created.Status);
        Assert.NotNull(created.ProcessedAt);
    }

    [Fact]
    public async Task Reject_Then_ReleaseSeats_Should_Restore_AvailableSeats()
    {
        using var scope = CreateProvider(totalSeats: 3).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await booking.CreateBookingAsync(1, UserId);
        var ev = await events.GetEventAsync(1);

        created.Reject();
        ev!.ReleaseSeats();

        Assert.Equal(3, ev.AvailableSeats);
    }

    [Fact]
    public async Task Reject_Then_ReleaseSeats_Should_Allow_New_Booking()
    {
        using var scope = CreateProvider(totalSeats: 1).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        created.Reject();
        (await events.GetEventAsync(1))!.ReleaseSeats();

        var next = await booking.CreateBookingAsync(1, UserId);

        Assert.NotEqual(created.Id, next.Id);
        Assert.Equal(BookingStatus.Pending, next.Status);
        Assert.Equal(0, (await events.GetEventAsync(1))!.AvailableSeats);
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Reflect_Rejected_Status()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        created.Reject();

        var result = await booking.GetBookingByIdAsync(created.Id);

        Assert.NotNull(result);
        Assert.Equal(BookingStatus.Rejected, result.Status);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Event_Not_Found()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<NotFoundException>(() => booking.CreateBookingAsync(999, UserId));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Event_Deleted()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();
        await events.DeleteEventAsync(1);

        await Assert.ThrowsAsync<NotFoundException>(() => booking.CreateBookingAsync(1, UserId));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_No_Available_Seats()
    {
        using var scope = CreateProvider(totalSeats: 1).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();
        (await events.GetEventAsync(1))!.AvailableSeats = 0;

        await Assert.ThrowsAsync<NoAvailableSeatsException>(() => booking.CreateBookingAsync(1, UserId));
    }

    [Fact]
    public async Task GetBookingByIdAsync_Should_Return_Null_When_Not_Found()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        var result = await booking.GetBookingByIdAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Prevent_Overbooking_Under_Concurrency()
    {
        var provider = CreateProvider(totalSeats: 5);

        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                try
                {
                    return await bookingService.CreateBookingAsync(1, UserId);
                }
                catch (NoAvailableSeatsException)
                {
                    return null;
                }
            }));

        var results = await Task.WhenAll(tasks);
        var succeeded = results.Where(x => x is not null).ToList();

        using var readScope = provider.CreateScope();
        var events = readScope.ServiceProvider.GetRequiredService<IEventService>();

        Assert.Equal(5, succeeded.Count);
        Assert.Equal(15, results.Count(x => x is null));
        Assert.Equal(0, (await events.GetEventAsync(1))!.AvailableSeats);
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Assign_Unique_Ids_Under_Concurrency()
    {
        var provider = CreateProvider(totalSeats: 10);

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => Task.Run(async () =>
            {
                using var scope = provider.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                return await bookingService.CreateBookingAsync(1, UserId);
            }));

        var results = await Task.WhenAll(tasks);

        Assert.Equal(10, results.Length);
        Assert.Equal(10, results.Select(x => x.Id).Distinct().Count());
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Event_Already_Started()
    {
        using var scope = CreateProvider(startAt: DateTime.UtcNow.AddHours(-1)).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        await Assert.ThrowsAsync<EventAlreadyPassedException>(() => booking.CreateBookingAsync(1, UserId));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Throw_When_Active_Bookings_Limit_Reached()
    {
        using var scope = CreateProvider(totalSeats: 15).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();

        for (var i = 0; i < BookingService.ActiveBookingsLimit; i++)
            await booking.CreateBookingAsync(1, UserId);

        await Assert.ThrowsAsync<ActiveBookingsLimitExceededException>(
            () => booking.CreateBookingAsync(1, UserId));
    }

    [Fact]
    public async Task CreateBookingAsync_Should_Not_Apply_Other_Users_Active_Limit()
    {
        using var scope = CreateProvider(totalSeats: 15).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        for (var i = 0; i < BookingService.ActiveBookingsLimit; i++)
            await booking.CreateBookingAsync(1, UserId);

        var other = await booking.CreateBookingAsync(1, otherUserId);

        Assert.Equal(otherUserId, other.UserId);
        Assert.Equal(BookingStatus.Pending, other.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Cancel_Own_Booking()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        var cancelled = await booking.CancelBookingAsync(created.Id, UserId, isAdmin: false);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.ProcessedAt);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Throw_When_User_Cancels_Foreign_Booking()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);
        var otherUserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        await Assert.ThrowsAsync<AccessDeniedException>(
            () => booking.CancelBookingAsync(created.Id, otherUserId, isAdmin: false));
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Allow_Admin_To_Cancel_Foreign_Booking()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);
        var adminId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        var cancelled = await booking.CancelBookingAsync(created.Id, adminId, isAdmin: true);

        Assert.Equal(BookingStatus.Cancelled, cancelled.Status);
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Throw_When_Already_Cancelled()
    {
        using var scope = CreateProvider().CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var created = await booking.CreateBookingAsync(1, UserId);
        await booking.CancelBookingAsync(created.Id, UserId, isAdmin: false);

        await Assert.ThrowsAsync<CancelledValidationException>(
            () => booking.CancelBookingAsync(created.Id, UserId, isAdmin: false));
    }

    [Fact]
    public async Task CancelBookingAsync_Should_Restore_AvailableSeats()
    {
        using var scope = CreateProvider(totalSeats: 5).CreateScope();
        var booking = scope.ServiceProvider.GetRequiredService<IBookingService>();
        var events = scope.ServiceProvider.GetRequiredService<IEventService>();
        var created = await booking.CreateBookingAsync(1, UserId);

        await booking.CancelBookingAsync(created.Id, UserId, isAdmin: false);

        Assert.Equal(5, (await events.GetEventAsync(1))!.AvailableSeats);
    }
}
