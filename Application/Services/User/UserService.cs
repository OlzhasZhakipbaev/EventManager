using Application.Repositories;
using Application.Security;
using Domain.Exceptions;
using Domain.Models;
using Domain.Models.Enums;

namespace Application.Services.User;

public class UserService : IUserService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;

    public UserService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<UserModel> RegisterAsync(string login, string password, Roles role = Roles.User)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            throw new DomainValidationException("Логин и пароль обязательны");

        if (await _users.ExistsByLoginAsync(login.Trim()))
            throw new DomainValidationException("Логин уже занят");

        var user = UserModel.Create(login, _hasher.Hash(password), role);
        await _users.AddAsync(user);
        await _users.SaveChangesAsync();
        return user;
    }

    public async Task<string> LoginAsync(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            throw new InvalidCredentialsException();

        var user = await _users.GetByLoginAsync(login.Trim());
        if (user is null || !_hasher.Verify(password, user.PasswordHash))
            throw new InvalidCredentialsException();

        return _jwt.CreateToken(user.Id, user.Login, user.Role);
    }
}
