using System.Text.Json;
using Events.Application.Caching;
using Xunit;
using Events.Application.Repositories;
using Events.Application.Services;
using Events.Domain.Models;

namespace Events.Application.Tests;

public class EventServiceCacheTests
{
    private static readonly TimeSpan EventTtl = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan TopTtl = TimeSpan.FromMinutes(5);

    [Fact]
    public async Task GetEvent_CacheHit_DoesNotCallRepository()
    {
        var stored = Sample(1, "Jazz");
        var (service, events, cache, _) = Create();
        cache.Data[CacheKeys.Event(1)] = JsonSerializer.Serialize(stored);

        var result = await service.GetEventAsync(1);

        Assert.Equal("Jazz", result!.Title);
        Assert.Equal(0, events.GetByIdCalls);
        Assert.Empty(cache.Sets);
    }

    [Fact]
    public async Task GetEvent_CacheMiss_LoadsRepositoryAndStoresCache()
    {
        var (service, events, cache, _) = Create();
        events.Stored = Sample(1, "Jazz");

        var result = await service.GetEventAsync(1);

        Assert.Equal("Jazz", result!.Title);
        Assert.Equal(1, events.GetByIdCalls);
        var set = Assert.Single(cache.Sets);
        Assert.Equal(CacheKeys.Event(1), set.Key);
        Assert.Equal(EventTtl, set.Ttl);
        Assert.Equal("Jazz", JsonSerializer.Deserialize<EventModel>(set.Value)!.Title);
    }

    [Fact]
    public async Task GetTopEvents_CacheHit_DoesNotCallRepository()
    {
        var (service, events, cache, _) = Create();
        cache.Data[CacheKeys.TopEvents] = JsonSerializer.Serialize(new List<EventModel> { Sample(1, "Jazz") });

        var result = await service.GetTopEventsAsync();

        Assert.Equal("Jazz", Assert.Single(result).Title);
        Assert.Equal(0, events.TopCalls);
        Assert.Empty(cache.Sets);
    }

    [Fact]
    public async Task GetTopEvents_CacheMiss_LoadsRepositoryAndStoresCache()
    {
        var (service, events, cache, _) = Create();
        events.Top = [Sample(2, "Rock")];

        var result = await service.GetTopEventsAsync();

        Assert.Equal("Rock", Assert.Single(result).Title);
        Assert.Equal(1, events.TopCalls);
        var set = Assert.Single(cache.Sets);
        Assert.Equal(CacheKeys.TopEvents, set.Key);
        Assert.Equal(TopTtl, set.Ttl);
    }

    [Fact]
    public async Task ChangeEvent_InvalidatesEventKeyAfterSave()
    {
        var (service, events, cache, operations) = Create();
        events.Stored = Sample(1, "Jazz");

        var changed = await service.ChangeEventAsync(1, Sample(1, "Blues"));

        Assert.True(changed);
        Assert.Equal("Blues", events.Stored!.Title);
        Assert.Equal(["save", "remove:event:1"], operations);
        Assert.DoesNotContain(CacheKeys.TopEvents, cache.Removes);
    }

    [Fact]
    public async Task DeleteEvent_InvalidatesEventKeyAfterSave()
    {
        var (service, events, cache, operations) = Create();
        events.Stored = Sample(1, "Jazz");

        var deleted = await service.DeleteEventAsync(1);

        Assert.True(deleted);
        Assert.True(events.Removed);
        Assert.Equal(["save", "remove:event:1"], operations);
        Assert.DoesNotContain(CacheKeys.TopEvents, cache.Removes);
    }

    [Fact]
    public async Task ReserveSeat_InvalidatesEventKeyAfterSave()
    {
        var (service, events, _, operations) = Create();
        events.Stored = Sample(1, "Jazz");

        var reserved = await service.ReserveSeatAsync(1);

        Assert.True(reserved);
        Assert.Equal(1, events.Stored!.AvailableSeats);
        Assert.Equal(["save", "remove:event:1"], operations);
    }

    [Fact]
    public async Task ReleaseSeat_InvalidatesEventKeyAfterSave()
    {
        var (service, events, _, operations) = Create();
        var stored = Sample(1, "Jazz");
        stored.TryReserveSeats();
        events.Stored = stored;

        await service.ReleaseSeatAsync(1);

        Assert.Equal(2, events.Stored!.AvailableSeats);
        Assert.Equal(["save", "remove:event:1"], operations);
    }

    [Fact]
    public async Task ChangeEvent_WhenMissing_DoesNotTouchCache()
    {
        var (service, _, _, operations) = Create();

        var changed = await service.ChangeEventAsync(1, Sample(1, "Blues"));

        Assert.False(changed);
        Assert.Empty(operations);
    }

    private static (EventService Service, FakeEvents Events, FakeCache Cache, List<string> Operations) Create()
    {
        var operations = new List<string>();
        var events = new FakeEvents(operations);
        var cache = new FakeCache(operations);
        var service = new EventService(events, cache, new CacheTtlOptions(EventTtl, TopTtl));
        return (service, events, cache, operations);
    }

    private static EventModel Sample(int id, string title) =>
        EventModel.Create(id, title, null, DateTime.UtcNow, DateTime.UtcNow.AddHours(2), 2);

    private sealed class FakeEvents : IEventRepository
    {
        public EventModel? Stored { get; set; }
        public IReadOnlyList<EventModel> Top { get; set; } = [];
        public int GetByIdCalls { get; private set; }
        public int TopCalls { get; private set; }
        public bool Removed { get; private set; }
        public List<string> Operations { get; }

        public FakeEvents(List<string> operations) => Operations = operations;

        public Task<EventModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            GetByIdCalls++;
            return Task.FromResult(Stored);
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<(IReadOnlyList<EventModel> Items, int TotalCount)> GetPagedAsync(
            string? title, DateTime? from, DateTime? to, int skip, int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<EventModel>, int)>(([], 0));

        public Task AddAsync(EventModel eventModel, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Remove(EventModel eventModel) => Removed = true;

        public Task<IReadOnlyList<EventModel>> GetTopBySoldPercentAsync(int take, CancellationToken cancellationToken = default)
        {
            TopCalls++;
            return Task.FromResult(Top);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            Operations.Add("save");
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCache : ICache
    {
        public Dictionary<string, string> Data { get; } = new();
        public List<(string Key, string Value, TimeSpan Ttl)> Sets { get; } = [];
        public List<string> Removes { get; } = [];
        public List<string> Operations { get; }

        public FakeCache(List<string> operations) => Operations = operations;

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.TryGetValue(key, out var value);
            return Task.FromResult(value);
        }

        public Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            Data[key] = value;
            Sets.Add((key, value, ttl));
            Operations.Add($"set:{key}");
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Data.Remove(key);
            Removes.Add(key);
            Operations.Add($"remove:{key}");
            return Task.CompletedTask;
        }
    }
}
