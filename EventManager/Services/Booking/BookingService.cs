using System.ComponentModel.DataAnnotations;
using EventManager.Exceptions;
using EventManager.Models;
using EventManager.Models.Enums;
using EventManager.Services.Event;

namespace EventManager.Services.Booking;

public class BookingService : IBookingService
{
    private readonly List<BookingModel> _bookings = [];
    private readonly IEventService _eventService;
    
    public BookingService(IEventService eventService)
    {
        _eventService = eventService;
    }
    
    public Task<BookingModel> CreateBookingAsync(int eventId)
    {
        var ev = _eventService.GetEvent(eventId);
        
        if (ev is null)
            throw new NotFoundException("Событие не найдено");

        lock (_bookings)
        {
            var hasActive = _bookings.Any(x =>
                x.EventId == eventId &&
                x.Status != BookingStatus.Rejected);
            
            if (hasActive)
                throw new ValidationException("Событие уже забронировано");
            
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
        lock (_bookings)
        { 
            var booking = _bookings.FirstOrDefault(x => x.Id == bookingId);
            return Task.FromResult(booking);
        }
    }
    
    public async Task ProcessPendingAsync(CancellationToken ct)
    {
        List<BookingModel> pending;

        lock (_bookings)
        {
            pending = _bookings.Where(x=> x.Status == BookingStatus.Pending).ToList();
        }

        foreach (var booking in pending)
        {
            await Task.Delay(2000, ct);
            lock(_bookings)
            {
                booking.Status = BookingStatus.Confirmed;
                booking.ProcessedAt = DateTime.UtcNow;
            }
        }
    }
}