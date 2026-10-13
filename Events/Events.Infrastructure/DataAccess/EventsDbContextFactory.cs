using Events.Infrastructure.DataAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Events.Infrastructure.DataAccess;

public sealed class EventsDbContextFactory : IDesignTimeDbContextFactory<EventsDbContext>
{
    public EventsDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<EventsDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=events_db;Username=postgres;Password=postgres")
            .Options;
        return new EventsDbContext(options);
    }
}
