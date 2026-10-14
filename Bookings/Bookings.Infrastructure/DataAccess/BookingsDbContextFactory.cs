using Bookings.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Bookings.Infrastructure.DataAccess;

public sealed class BookingsDbContextFactory : IDesignTimeDbContextFactory<BookingsDbContext>
{
    public BookingsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BookingsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=bookings_db;Username=postgres;Password=postgres")
            .Options;
        return new BookingsDbContext(options);
    }
}
