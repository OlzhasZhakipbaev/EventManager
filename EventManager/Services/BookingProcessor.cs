using EventManager.Repositories;

namespace EventManager.Services;

public class BookingProcessor : BackgroundService
{
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
        var events = scope.ServiceProvider.GetRequiredService<IEventRepository>();
        var booking = await bookings.GetByIdAsync(bookingId, stoppingToken);
        if (booking is null)
            return;

        try
        {
            var ev = await events.GetByIdAsync(booking.EventId, stoppingToken);
            if (ev is null)
            {
                booking.Reject();
                await bookings.SaveChangesAsync(stoppingToken);
                _logger.LogWarning(
                    "Событие {EventId} не найдено, резерв {BookingId} отклонен",
                    booking.EventId,
                    booking.Id);
                return;
            }

            booking.Confirm();
            await bookings.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            var ev = await events.GetByIdAsync(booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await bookings.SaveChangesAsync();
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = await events.GetByIdAsync(booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await bookings.SaveChangesAsync();
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}
