using EventManager.DataAccess;
using EventManager.Models;
using Microsoft.EntityFrameworkCore;

namespace EventManager.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _context;

    public EventRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<EventModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Events.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return _context.Events.AnyAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(IReadOnlyList<EventModel> Items, int TotalCount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Events.AsQueryable();

        if (!string.IsNullOrEmpty(title))
        {
            var filter = title.ToLower();
            query = query.Where(x => x.Title.ToLower().Contains(filter));
        }

        if (from.HasValue)
            query = query.Where(x => x.StartAt >= from.Value);

        if (to.HasValue)
            query = query.Where(x => x.EndAt <= to.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(x => x.Id).Skip(skip).Take(take).ToListAsync(cancellationToken);
        return (items, totalCount);
    }

    public async Task AddAsync(EventModel eventModel, CancellationToken cancellationToken = default)
    {
        await _context.Events.AddAsync(eventModel, cancellationToken);
    }

    public void Remove(EventModel eventModel)
    {
        _context.Events.Remove(eventModel);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
