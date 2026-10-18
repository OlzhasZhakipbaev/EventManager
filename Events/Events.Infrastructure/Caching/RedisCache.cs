using Events.Application.Caching;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Events.Infrastructure.Caching;

public sealed class RedisCache : ICache
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCache> _logger;

    public RedisCache(IConnectionMultiplexer redis, ILogger<RedisCache> logger)
    {
        _redis = redis;
        _logger = logger;
    }

    public async Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var value = await _redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? value.ToString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось прочитать ключ {Key} из Redis", key);
            return null;
        }
    }

    public async Task SetAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().StringSetAsync(key, value, ttl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось записать ключ {Key} в Redis", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _redis.GetDatabase().KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось удалить ключ {Key} из Redis", key);
        }
    }
}
