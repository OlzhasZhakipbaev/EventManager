using Domain.Models.Enums;

namespace Application.Security;

public interface IJwtTokenService
{
    string CreateToken(Guid userId, string login, Roles role);
}
