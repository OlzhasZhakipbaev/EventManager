using Application.Services;
using Application.Services.Booking;
using Application.Services.Event;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddScoped<IEventService, EventService>();
        services.AddHostedService<BookingProcessor>();

        return services;
    }
}