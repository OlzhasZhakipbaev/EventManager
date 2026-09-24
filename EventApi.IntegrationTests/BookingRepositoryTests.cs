using EventManager.DataAccess;
using EventManager.Models;
using EventManager.Models.Enums;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventApi.IntegrationTests;

[Collection("Postgres")]
public class BookingRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private AppDbContext _context = null!;
    private EventRepository _events = null!;
    private BookingRepository _bookings = null!;

    public BookingRepositoryTests(PostgresFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        NpgsqlConnection.ClearAllPools();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(_fixture.ConnectionString)
            .Options);
        await _context.Database.EnsureDeletedAsync();
        await _context.Database.MigrateAsync();
        _events = new EventRepository(_context);
        _bookings = new BookingRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private async Task SeedEvent(int id = 1)
    {
        await _events.AddAsync(EventModel.Create(
            id,
            "Conference",
            "desc",
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 1, 11, 0, 0, 0, DateTimeKind.Utc),
            10));
        await _events.SaveChangesAsync();
    }

    [Fact]
    public async Task Add_Then_GetById_Returns_Saved_Booking()
    {
        await SeedEvent();
        var booking = BookingModel.Create(1);

        await _bookings.AddAsync(booking);
        await _bookings.SaveChangesAsync();

        var loaded = await _bookings.GetByIdAsync(booking.Id);

        Assert.NotNull(loaded);
        Assert.Equal(booking.Id, loaded.Id);
        Assert.Equal(1, loaded.EventId);
        Assert.Equal(BookingStatus.Pending, loaded.Status);
        Assert.Null(loaded.ProcessedAt);
    }

    [Fact]
    public async Task GetById_Returns_Null_When_Missing()
    {
        Assert.Null(await _bookings.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPending_Returns_Only_Pending()
    {
        await SeedEvent();
        var pending = BookingModel.Create(1);
        var confirmed = BookingModel.Create(1);
        confirmed.Confirm();

        await _bookings.AddAsync(pending);
        await _bookings.AddAsync(confirmed);
        await _bookings.SaveChangesAsync();

        var result = await _bookings.GetPendingAsync();

        Assert.Single(result);
        Assert.Equal(pending.Id, result[0].Id);
        Assert.Equal(BookingStatus.Pending, result[0].Status);
    }

    [Fact]
    public async Task GetPendingIds_Returns_Only_Pending_Ids()
    {
        await SeedEvent();
        var pending = BookingModel.Create(1);
        var rejected = BookingModel.Create(1);
        rejected.Reject();

        await _bookings.AddAsync(pending);
        await _bookings.AddAsync(rejected);
        await _bookings.SaveChangesAsync();

        var ids = await _bookings.GetPendingIdsAsync();

        Assert.Equal(pending.Id, Assert.Single(ids));
    }

    [Fact]
    public async Task SaveChanges_Persists_Status_Update()
    {
        await SeedEvent();
        var booking = BookingModel.Create(1);
        await _bookings.AddAsync(booking);
        await _bookings.SaveChangesAsync();

        booking.Confirm();
        await _bookings.SaveChangesAsync();

        var loaded = await _bookings.GetByIdAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, loaded!.Status);
        Assert.NotNull(loaded.ProcessedAt);
        Assert.Empty(await _bookings.GetPendingAsync());
    }
}
