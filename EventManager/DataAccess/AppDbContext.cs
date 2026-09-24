using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.DataAccess;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<EventModel> Events => Set<EventModel>();
    public DbSet<BookingModel> Bookings => Set<BookingModel>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
