using EventManager.DataAccess;
using EventManager.Models.Enums;
using Microsoft.EntityFrameworkCore;

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
            List<Guid> pendingIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                pendingIds = await context.Bookings
                    .Where(x => x.Status == BookingStatus.Pending)
                    .Select(x => x.Id)
                    .ToListAsync(stoppingToken);
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
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var booking = await context.Bookings.FirstOrDefaultAsync(x => x.Id == bookingId, stoppingToken);
        if (booking is null)
            return;

        try
        {
            var ev = await context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId, stoppingToken);
            if (ev is null)
            {
                booking.Reject();
                await context.SaveChangesAsync(stoppingToken);
                _logger.LogWarning(
                    "Событие {EventId} не найдено, резерв {BookingId} отклонен",
                    booking.EventId,
                    booking.Id);
                return;
            }

            booking.Confirm();
            await context.SaveChangesAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            var ev = await context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await context.SaveChangesAsync();
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = await context.Events.FirstOrDefaultAsync(x => x.Id == booking.EventId);
            if (ev is not null)
                ev.ReleaseSeats();

            await context.SaveChangesAsync();
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}
