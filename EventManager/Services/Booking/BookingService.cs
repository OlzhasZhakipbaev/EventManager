using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Models.Enums;
using EventManager.Services.Event;
using Microsoft.Extensions.Logging.Abstractions;

namespace EventManager.Services.Booking;

public class BookingService : IBookingService
{
    private readonly List<BookingModel> _bookings = [];
    private readonly IEventService _eventService;
    private readonly object _bookingLock = new();
    private readonly ILogger _logger;
    
    public BookingService(IEventService eventService, ILogger<BookingService>? logger = null)
    {
        _eventService = eventService;
        _logger = logger ?? NullLogger<BookingService>.Instance;
    }
    
    public Task<BookingModel> CreateBookingAsync(int eventId)
    {
        lock (_bookingLock)
        {
            var ev = _eventService.GetEvent(eventId);

            if (ev is null)
                throw new NotFoundException("Событие не найдено");

            if (!ev.TryReserveSeats())
                throw new NoAvailableSeatsException("Нет свободны мест для этого события");

            _eventService.ChangeEvent(eventId, ev);

            var booking = new BookingModel
            {
                Id = Guid.NewGuid(),
                Status = BookingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ProcessedAt = null,
                EventId = eventId
            };

            _bookings.Add(booking);

            return Task.FromResult(booking);
        }
    }
    
    public Task<BookingModel?> GetBookingByIdAsync(Guid bookingId)
    {
        var booking = _bookings.FirstOrDefault(x => x.Id == bookingId);
        return Task.FromResult(booking);
    }
    
    public IEnumerable<BookingModel> GetPending()
    {
        lock (_bookingLock)
        {
            return _bookings.Where(x => x.Status == BookingStatus.Pending).ToList();
        }
    }

    public void Update(BookingModel booking)
    {
    }

    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        var pendingBookings = GetPending().ToList();
        var tasks = pendingBookings.Select(booking => ProcessBookingAsync(booking, ct));
        await Task.WhenAll(tasks);
    }

    private async Task ProcessBookingAsync(BookingModel booking, CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(2000, stoppingToken);
            
            var ev = _eventService.GetEvent(booking.EventId);
            if (ev is null)
            {
                    booking.Reject();
                    Update(booking);
                    _logger.LogWarning(
                        "Событие {EventId} не найдено, резерв {BookingId} отклонено",
                        booking.EventId,
                        booking.Id);
                    return;
            }
            booking.Confirm();
            Update(booking);
        }
        catch (OperationCanceledException)
        {
            booking.Reject();
            var ev = _eventService.GetEvent(booking.EventId);
            if (ev is not null)
            {
                ev.ReleaseSeats();
                _eventService.ChangeEvent(booking.EventId, ev);
            }
            Update(booking);
            throw;
        }
        catch (Exception ex)
        {
            booking.Reject();
            var ev = _eventService.GetEvent(booking.EventId);
            if (ev is not null)
            {
                ev.ReleaseSeats();
                _eventService.ChangeEvent(booking.EventId, ev);
            }

            Update(booking);
            _logger.LogError(ex, "Unexpected error processing booking {BookingId}", booking.Id);
        }
    }
}