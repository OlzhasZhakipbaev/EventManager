using EventManager.Models;

namespace EventManager.Services.Booking;

public interface IBookingService
{
   Task ProcessPendingAsync(CancellationToken ct);
   Task<BookingModel> CreateBookingAsync(int eventId);
   Task<BookingModel?> GetBookingByIdAsync(Guid bookingId);
}