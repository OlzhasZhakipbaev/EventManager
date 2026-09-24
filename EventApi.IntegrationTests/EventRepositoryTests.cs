using EventManager.DataAccess;
using EventManager.Models;
using EventManager.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EventApi.IntegrationTests;

[Collection("Postgres")]
public class EventRepositoryTests : IAsyncLifetime
{
    private readonly PostgresFixture _fixture;
    private AppDbContext _context = null!;
    private EventRepository _repository = null!;

    public EventRepositoryTests(PostgresFixture fixture)
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
        _repository = new EventRepository(_context);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
    }

    private static EventModel Event(
        int id,
        string title,
        DateTime start,
        DateTime end,
        int seats = 10)
    {
        return EventModel.Create(id, title, "desc", start, end, seats);
    }

    private static DateTime Utc(int year, int month, int day) =>
        new(year, month, day, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Add_Then_GetById_Returns_Saved_Event()
    {
        var created = Event(1, "Conference", Utc(2026, 1, 10), Utc(2026, 1, 11), 20);
        await _repository.AddAsync(created);
        await _repository.SaveChangesAsync();

        var loaded = await _repository.GetByIdAsync(1);

        Assert.NotNull(loaded);
        Assert.Equal("Conference", loaded.Title);
        Assert.Equal(20, loaded.TotalSeats);
        Assert.Equal(20, loaded.AvailableSeats);
    }

    [Fact]
    public async Task GetById_Returns_Null_When_Missing()
    {
        Assert.Null(await _repository.GetByIdAsync(999));
    }

    [Fact]
    public async Task Exists_Returns_True_Only_For_Saved_Id()
    {
        await _repository.AddAsync(Event(1, "Conference", Utc(2026, 1, 10), Utc(2026, 1, 11)));
        await _repository.SaveChangesAsync();

        Assert.True(await _repository.ExistsAsync(1));
        Assert.False(await _repository.ExistsAsync(2));
    }

    [Fact]
    public async Task Remove_Deletes_Event()
    {
        var ev = Event(1, "Conference", Utc(2026, 1, 10), Utc(2026, 1, 11));
        await _repository.AddAsync(ev);
        await _repository.SaveChangesAsync();

        _repository.Remove(ev);
        await _repository.SaveChangesAsync();

        Assert.Null(await _repository.GetByIdAsync(1));
        Assert.False(await _repository.ExistsAsync(1));
    }

    [Fact]
    public async Task GetPaged_Without_Filters_Returns_All()
    {
        await SeedThreeEvents();

        var (items, total) = await _repository.GetPagedAsync(null, null, null, 0, 10);

        Assert.Equal(3, total);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public async Task GetPaged_Filters_By_Title()
    {
        await SeedThreeEvents();

        var (items, total) = await _repository.GetPagedAsync("Meet", null, null, 0, 10);

        Assert.Equal(1, total);
        Assert.Equal("Meeting", Assert.Single(items).Title);
    }

    [Fact]
    public async Task GetPaged_Filters_By_Date_Range()
    {
        await SeedThreeEvents();

        var (items, total) = await _repository.GetPagedAsync(
            null,
            Utc(2026, 2, 1),
            Utc(2026, 2, 28),
            0,
            10);

        Assert.Equal(1, total);
        Assert.Equal("Meeting", Assert.Single(items).Title);
    }

    [Fact]
    public async Task GetPaged_Filters_By_Title_And_Date()
    {
        await SeedThreeEvents();

        var (items, total) = await _repository.GetPagedAsync(
            "Meet",
            Utc(2026, 2, 1),
            Utc(2026, 2, 28),
            0,
            10);

        Assert.Equal(1, total);
        Assert.Equal("Meeting", Assert.Single(items).Title);
    }

    [Fact]
    public async Task GetPaged_Applies_Pagination()
    {
        await SeedThreeEvents();

        var (page1, total) = await _repository.GetPagedAsync(null, null, null, 0, 2);
        var (page2, _) = await _repository.GetPagedAsync(null, null, null, 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(2, page1.Count);
        Assert.Single(page2);
        Assert.Equal("Workshop", page2[0].Title);
    }

    private async Task SeedThreeEvents()
    {
        await _repository.AddAsync(Event(1, "Conference", Utc(2026, 1, 10), Utc(2026, 1, 11)));
        await _repository.AddAsync(Event(2, "Meeting", Utc(2026, 2, 10), Utc(2026, 2, 11)));
        await _repository.AddAsync(Event(3, "Workshop", Utc(2026, 3, 10), Utc(2026, 3, 11)));
        await _repository.SaveChangesAsync();
    }
}
