using Users.Domain.Models;

namespace Users.Application.Repositories;

public interface IUserRepository
{
    Task<UserModel?> GetByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default);
    Task AddAsync(UserModel user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
