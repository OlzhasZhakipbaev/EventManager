using Application.Repositories;
using Application.Security;
using Infrastructure.DataAccess;
using Infrastructure.Repositories;
using Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var jwtSettings = new JwtSettings
        {
            Secret = jwtSection["Secret"] ?? "",
            Issuer = jwtSection["Issuer"] ?? "",
            Audience = jwtSection["Audience"] ?? "",
            ExpirationMinutes = int.TryParse(jwtSection["ExpirationMinutes"], out var minutes) ? minutes : 60
        };
        services.AddSingleton(Options.Create(jwtSettings));
        services.AddSingleton<IPasswordHasher, Sha256PasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        services.AddScoped<IBookingRepository, BookingRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}