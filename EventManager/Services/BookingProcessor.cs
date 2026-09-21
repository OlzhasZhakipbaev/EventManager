using EventManager.Models;
using EventManager.Services.Booking;
using EventManager.Services.Event;

namespace EventManager.Services;

public class BookingProcessor : BackgroundService
{
    private readonly IBookingService _bookingStore;
    private readonly IEventService _eventStore;
    private readonly ILogger<BookingProcessor> _logger;

    public BookingProcessor(
        IBookingService bookingStore,
        IEventService eventStore,
        ILogger<BookingProcessor> logger)
    {
        _bookingStore = bookingStore;
        _eventStore = eventStore;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var pendingBookings = _bookingStore.GetPending().ToList();
            var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, stoppingToken));
            await Task.WhenAll(tasks);
            await Task.Delay(2000, stoppingToken);
        }
    }

    private async Task ProcessBookingAsync(BookingModel booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);

            var ev = _eventStore.GetEvent(booking.EventId);
            if (ev is null)
            {
                booking.Reject();
                _bookingStore.Update(booking);
                _logger.LogWarning(
                    "Событие {EventId} не найдено, резерв {BookingId} отклонен",
                    booking.EventId,
                    booking.Id);
                return;
            }

            booking.Confirm();
            _bookingStore.Update(booking);
        }
        catch (OperationCanceledException)
        {
           booking.Reject();
           var ev = _eventStore.GetEvent(booking.EventId);
           if (ev is not null)
           {
               ev.ReleaseSeats();
               _eventStore.ChangeEvent(booking.EventId, ev);
           }
           _bookingStore.Update(booking);
           throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = _eventStore.GetEvent(booking.EventId);
            if (ev is not null)
            {
                ev.ReleaseSeats();
                _eventStore.ChangeEvent(booking.EventId, ev);
            }

            _bookingStore.Update(booking);
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}
