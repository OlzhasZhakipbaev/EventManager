using EventManager.Models;

namespace EventManager.Repositories;

public interface IBookingRepository
{
    Task<BookingModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BookingModel>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default);
    Task AddAsync(BookingModel booking, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
