using Bookings.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Bookings.Infrastructure.DataAccess;

public sealed class BookingsDbContext : DbContext
{
    public BookingsDbContext(DbContextOptions<BookingsDbContext> options) : base(options)
    {
    }

    public DbSet<BookingModel> Bookings => Set<BookingModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BookingsDbContext).Assembly);
    }
}
