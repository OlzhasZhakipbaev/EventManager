using Users.Domain.Models.Enums;

namespace Users.Domain.Models;

public class UserModel
{
    private UserModel()
    {
    }

    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Roles Role { get; private set; }

    public static UserModel Create(string login, string passwordHash, Roles role = Roles.User)
    {
        return new UserModel
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
