namespace Events.Application.Caching;

public static class CacheKeys
{
    public const string TopEvents = "events:top10";

    public static string Event(int id) => $"event:{id}";
}

public interface ICache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
