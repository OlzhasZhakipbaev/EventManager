using Application.Repositories;
using Infrastructure.DataAccess;
using Domain.Models;
using Domain.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class BookingRepository : IBookingRepository
{
    private readonly AppDbContext _context;

    public BookingRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<BookingModel?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Bookings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<BookingModel>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(x => x.Status == BookingStatus.Pending)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetPendingIdsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Bookings
            .Where(x => x.Status == BookingStatus.Pending)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(BookingModel booking, CancellationToken cancellationToken = default)
    {
        await _context.Bookings.AddAsync(booking, cancellationToken);
    }

    public Task<int> CountActiveByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Bookings.CountAsync(
            x => x.UserId == userId
                 && (x.Status == BookingStatus.Pending || x.Status == BookingStatus.Confirmed),
            cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
