using Microsoft.Extensions.Options;

namespace Events.Infrastructure.Configs.Redis;

public class RedisSettingsOptions : IConfigureNamedOptions<RedisSettings>
{
    public void Configure(RedisSettings options)
    {
        Configure("", options);
    }

    public void Configure(string? name, RedisSettings options)
    {
    }
}