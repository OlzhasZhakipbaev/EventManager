using Users.Domain.Models;
using Users.Domain.Models.Enums;

namespace Users.Application.Services;

public interface IUserService
{
    Task<UserModel> RegisterAsync(string login, string password, Roles role = Roles.User);
    Task<string> LoginAsync(string login, string password);
}
