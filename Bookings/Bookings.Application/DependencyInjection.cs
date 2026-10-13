using Bookings.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBookingService, BookingService>();
        services.AddHostedService<BookingProcessor>();
        return services;
    }
}
