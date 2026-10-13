using Users.Domain.Models.Enums;

namespace Users.Application.Security;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string login, Roles role);
}
