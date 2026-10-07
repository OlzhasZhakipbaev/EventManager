using Domain.Models;
using Domain.Models.Enums;

namespace Application.Services.User;

public interface IUserService
{
    Task<UserModel> RegisterAsync(string login, string password, Roles role = Roles.User);
    Task<string> LoginAsync(string login, string password);
}
