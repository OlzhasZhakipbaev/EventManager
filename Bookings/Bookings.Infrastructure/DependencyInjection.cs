using Bookings.Application.Messaging;
using Bookings.Application.Repositories;
using Bookings.Infrastructure.DataAccess;
using Bookings.Infrastructure.Messaging;
using Bookings.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bookings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<BookingsDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IBookingRepository, BookingRepository>();

        var bootstrapServers = configuration["Kafka:BootstrapServers"]
                               ?? throw new InvalidOperationException("Kafka:BootstrapServers не настроен.");
        services.AddSingleton<IBookingConfirmedPublisher>(_ => new KafkaBookingConfirmedPublisher(bootstrapServers));

        return services;
    }
}
