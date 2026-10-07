using Domain.Models.Enums;

namespace Domain.Models;

public class UserModel
{
    public Guid Id { get; private set; }
    public string Login { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public Roles Role { get; private set; }

    private UserModel() { }

    public static UserModel Create(string login, string passwordHash, Roles role = Roles.User)
    {
        if (string.IsNullOrWhiteSpace(login))
            throw new ArgumentException("Логин не может быть пустым.", nameof(login));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Хеш пароля не может быть пустым.", nameof(passwordHash));

        return new UserModel
        {
            Id = Guid.NewGuid(),
            Login = login.Trim(),
            PasswordHash = passwordHash,
            Role = role
        };
    }

    public bool IsAdmin => Role == Roles.Admin;
}