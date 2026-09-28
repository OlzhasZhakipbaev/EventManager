using EventManager.Models;

namespace EventManager.Repositories;

public interface IEventRepository
{
    Task<EventModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<EventModel> Items, int TotalCount)> GetPagedAsync(
        string? title,
        DateTime? from,
        DateTime? to,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task AddAsync(EventModel eventModel, CancellationToken cancellationToken = default);
    void Remove(EventModel eventModel);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
