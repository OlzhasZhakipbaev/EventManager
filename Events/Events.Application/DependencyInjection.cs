using Events.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Events.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IEventService, EventService>();
        return services;
    }
}
