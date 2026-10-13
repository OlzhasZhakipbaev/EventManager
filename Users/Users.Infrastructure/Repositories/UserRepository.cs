using Users.Application.Repositories;
using Users.Domain.Models;
using Users.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;

namespace Users.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UsersDbContext _context;

    public UserRepository(UsersDbContext context)
    {
        _context = context;
    }

    public Task<UserModel?> GetByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        return _context.Users.FirstOrDefaultAsync(x => x.Login == login, cancellationToken);
    }

    public Task<bool> ExistsByLoginAsync(string login, CancellationToken cancellationToken = default)
    {
        return _context.Users.AnyAsync(x => x.Login == login, cancellationToken);
    }

    public async Task AddAsync(UserModel user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
