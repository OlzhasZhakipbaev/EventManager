using Bookings.Application.Messaging;
using Bookings.Application.Repositories;
using Bookings.Domain.Models;
using Bookings.Domain.Models.Enums;
using Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bookings.Application.Services;

public class BookingProcessor : BackgroundService
{
    private const int SeatCount = 1;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(IServiceScopeFactory scopeFactory, ILogger<BookingProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IReadOnlyList<Guid> pendingIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var bookings = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
                pendingIds = await bookings.GetPendingIdsAsync(stoppingToken);
            }

            var tasks = pendingIds.Select(id => ProcessBookingAsync(id, stoppingToken));
            await Task.WhenAll(tasks);
            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(Guid bookingId, CancellationToken stoppingToken)
    {
        await Task.Delay(2000, stoppingToken);

        using var scope = _scopeFactory.CreateScope();
        var bookings = scope.ServiceProvider.GetRequiredService<IBookingRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IBookingConfirmedPublisher>();
        var booking = await bookings.GetByIdAsync(bookingId, stoppingToken);
        if (booking is null || booking.Status != BookingStatus.Pending)
            return;

        try
        {
            booking.Confirm();
            await bookings.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            await bookings.SaveChangesAsync(CancellationToken.None);
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            await bookings.SaveChangesAsync(CancellationToken.None);
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
            return;
        }

        await PublishConfirmedAsync(publisher, booking, stoppingToken);
    }

    private async Task PublishConfirmedAsync(
        IBookingConfirmedPublisher publisher,
        BookingModel booking,
        CancellationToken cancellationToken)
    {
        try
        {
            await publisher.PublishAsync(
                new BookingConfirmed(
                    booking.Id,
                    booking.EventId,
                    booking.UserId,
                    SeatCount,
                    booking.ProcessedAt ?? DateTime.UtcNow),
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Не удалось опубликовать подтверждение брони {BookingId}", booking.Id);
        }
    }
}
