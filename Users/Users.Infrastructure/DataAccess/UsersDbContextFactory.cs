using Users.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Users.Infrastructure.DataAccess;

public sealed class UsersDbContextFactory : IDesignTimeDbContextFactory<UsersDbContext>
{
    public UsersDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=users_db;Username=postgres;Password=postgres")
            .Options;
        return new UsersDbContext(options);
    }
}
