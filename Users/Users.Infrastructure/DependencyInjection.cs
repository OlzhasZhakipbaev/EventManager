using Users.Application.Repositories;
using Users.Application.Security;
using Users.Infrastructure.DataAccess;
using Users.Infrastructure.Repositories;
using Users.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Users.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UsersDbContext>(options =>
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
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
