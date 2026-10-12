using Domain.Exceptions;
using Domain.Models;
using Application.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Application.Services.Booking;

public class BookingService : IBookingService
{
    public const int ActiveBookingsLimit = 10;

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IEventRepository _events;
    private readonly IBookingRepository _bookings;
    private readonly ILogger _logger;

    public BookingService(
        IEventRepository events,
        IBookingRepository bookings,
        ILogger<BookingService>? logger = null)
    {
        _events = events;
        _bookings = bookings;
        _logger = logger ?? NullLogger<BookingService>.Instance;
    }

    public async Task<BookingModel> CreateBookingAsync(int eventId, Guid userId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var ev = await _events.GetByIdAsync(eventId);
            if (ev is null)
                throw new NotFoundException("Событие не найдено");

            if (ev.StartAt <= DateTime.UtcNow)
                throw new EventAlreadyPassedException(ev.Id, ev.StartAt);

            var activeCount = await _bookings.CountActiveByUserAsync(userId);
            if (activeCount >= ActiveBookingsLimit)
                throw new ActiveBookingsLimitExceededException(userId, ActiveBookingsLimit);

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет свободны мест для этого события");

            var booking = BookingModel.Create(eventId, userId);
            await _bookings.AddAsync(booking);
            await _bookings.SaveChangesAsync();
            return booking;
        }
        finally
        {
            BookingLock.Release();
        }
    }

    public async Task<BookingModel> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin)
    {
        var booking = await _bookings.GetByIdAsync(bookingId);
        if (booking is null)
            throw new NotFoundException("Бронирование не найдено");

        if (!isAdmin && booking.UserId != userId)
            throw new AccessDeniedException(userId, "cancel");

        booking.Cancel();

        var ev = await _events.GetByIdAsync(booking.EventId);
        if (ev is not null)
            ev.ReleaseSeats();

        await _bookings.SaveChangesAsync();
        return booking;
    }

    public async Task<BookingModel?> GetBookingByIdAsync(Guid bookingId, Guid? userId = null, bool isAdmin = false)
    {
        var booking = await _bookings.GetByIdAsync(bookingId);
        if (booking is null)
            return null;

        if (userId is not null && !isAdmin && booking.UserId != userId)
            throw new AccessDeniedException(userId.Value, "get");

        return booking;
    }

    public Task<IReadOnlyList<BookingModel>> GetPendingAsync()
    {
        return _bookings.GetPendingAsync();
    }

    public Task UpdateAsync(BookingModel booking)
    {
        return _bookings.SaveChangesAsync();
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var pendingBookings = await _bookings.GetPendingAsync(ct);
        foreach (var booking in pendingBookings)
            await ProcessBookingAsync(booking, ct);
    }

    private async Task ProcessBookingAsync(BookingModel booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);

            var ev = await _events.GetByIdAsync(booking.EventId, stoppingToken);
            if (ev is null)
            {
                booking.Reject();
                await _bookings.SaveChangesAsync(stoppingToken);
                _logger.LogWarning(
                    "Событие {EventId} не найдено, резерв {BookingId} отклонено",
                    booking.EventId,
                    booking.Id);
                return;
            }

            booking.Confirm();
            await _bookings.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            var ev = await _events.GetByIdAsync(booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await _bookings.SaveChangesAsync();
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = await _events.GetByIdAsync(booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await _bookings.SaveChangesAsync();
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}
