using Events.Application.Caching;
using Events.Application.Repositories;
using Events.Application.Services;
using Events.Infrastructure.Caching;
using Events.Infrastructure.Configs.Redis;
using Events.Infrastructure.DataAccess;
using Events.Infrastructure.Messaging;
using Events.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Events.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<EventsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IEventRepository, EventRepository>();
        services.AddHostedService<KafkaTopicInitializer>();
        services.AddHostedService<BookingConfirmedConsumer>();
        services.AddRedisInfra(configuration);
        return services;
    }

    public static IServiceCollection AddRedisInfra(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(RedisSettings.SectionName);

        if (!section.Exists())
            throw new InvalidOperationException("Redis section not found");

        var settings = section.Get<RedisSettings>()
                       ?? throw new InvalidOperationException("Invalid Redis configuration");

        if (string.IsNullOrWhiteSpace(settings.ConnectionString))
            throw new InvalidOperationException("Redis:ConnectionString is not configured");

        services.AddSingleton(new CacheTtlOptions(
            TimeSpan.FromMinutes(settings.EventTtlMinutes),
            TimeSpan.FromMinutes(settings.TopEventsTtlMinutes)));

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var config = ConfigurationOptions.Parse(settings.ConnectionString);
            config.AbortOnConnectFail = settings.AbortOnConnectFail;
            if (settings.ConnectTimeout > 0)
                config.ConnectTimeout = (int)settings.ConnectTimeout;
            if (settings.SyncTimeout > 0)
                config.SyncTimeout = (int)settings.SyncTimeout;

            return ConnectionMultiplexer.Connect(config);
        });

        services.AddSingleton<ICache, RedisCache>();
        return services;
    }
}
