using System.Security.Claims;
using System.Text;
using Application.Security;
using Domain.Models.Enums;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Security;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string CreateToken(Guid userId, string login, Roles role)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.UniqueName, login),
                new Claim(ClaimTypes.Role, role.ToString())
            }),
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            Expires = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
            SigningCredentials = credentials
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
