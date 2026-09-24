using EventManager.DataAccess;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManager.Services.Booking;

public class BookingService : IBookingService
{
    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly AppDbContext _context;
    private readonly ILogger _logger;

    public BookingService(AppDbContext context, ILogger<BookingService>? logger = null)
    {
        _context = context;
        _logger = logger ?? NullLogger<BookingService>.Instance;
    }

    public async Task<BookingModel> CreateBookingAsync(int eventId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var ev = await _context.Events.FirstOrDefaultAsync(x => x.Id == eventId);
            if (ev is null)
                throw new NotFoundException("Событие не найдено");

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет свободны мест для этого события");

            var booking = BookingModel.Create(eventId);
            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();
            return booking;
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public Task<BookingModel?> GetBookingByIdAsync(Guid bookingId)
    {
        return _context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId);
    }

    public async Task<IReadOnlyList<BookingModel>> GetPendingAsync()
    {
        return await _context.Bookings
            .Where(x => x.Status == BookingStatus.Pending)
            .ToListAsync();
    }

    public async Task UpdateAsync(BookingModel booking)
    {
        await _context.SaveChangesAsync();
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var pendingBookings = await GetPendingAsync();
        foreach (var booking in pendingBookings)
            await ProcessBookingAsync(booking, ct);
    }

    private async Task ProcessBookingAsync(BookingModel booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);

            var ev = await _context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId, stoppingToken);
            if (ev is null)
            {
                booking.Reject();
                await UpdateAsync(booking);
                _logger.LogWarning(
                    "Событие {EventId} не найдено, резерв {BookingId} отклонено",
                    booking.EventId,
                    booking.Id);
                return;
            }

            booking.Confirm();
            await UpdateAsync(booking);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            var ev = await _context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await UpdateAsync(booking);
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = await _context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await UpdateAsync(booking);
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}
