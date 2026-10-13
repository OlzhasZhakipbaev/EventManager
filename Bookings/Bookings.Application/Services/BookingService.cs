using Bookings.Application.Messaging;
using Bookings.Application.Repositories;
using Bookings.Domain.Exceptions;
using Bookings.Domain.Models;
using Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bookings.Application.Services;

public class BookingService : IBookingService
{
    public const int ActiveBookingsLimit = 10;
    private const int SeatCount = 1;

    private static readonly SemaphoreSlim BookingLock = new(1, 1);

    private readonly IBookingRepository _bookings;
    private readonly IBookingConfirmedPublisher _publisher;
    private readonly ILogger _logger;

    public BookingService(
        IBookingRepository bookings,
        IBookingConfirmedPublisher publisher,
        ILogger<BookingService>? logger = null)
    {
        _bookings = bookings;
        _publisher = publisher;
        _logger = logger ?? NullLogger<BookingService>.Instance;
    }

    public async Task<BookingModel> CreateBookingAsync(int eventId, Guid userId)
    {
        await BookingLock.WaitAsync();
        try
        {
            var activeCount = await _bookings.CountActiveByUserAsync(userId);
            if (activeCount >= ActiveBookingsLimit)
                throw new ActiveBookingsLimitExceededException(userId, ActiveBookingsLimit);

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
            booking.Confirm();
            await _bookings.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            await _bookings.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            await _bookings.SaveChangesAsync(CancellationToken.None);
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
            return;
        }

        try
        {
            await _publisher.PublishAsync(
                new BookingConfirmed(
                    booking.Id,
                    booking.EventId,
                    booking.UserId,
                    SeatCount,
                    booking.ProcessedAt ?? DateTime.UtcNow),
                stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось опубликовать подтверждение брони {BookingId}", booking.Id);
        }
    }
}
