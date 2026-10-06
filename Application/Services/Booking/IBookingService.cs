using Domain.Models;

namespace Application.Services.Booking;

public interface IBookingService
{
    Task ProcessPendingAsync(CancellationToken ct);
    Task<BookingModel> CreateBookingAsync(int eventId, Guid userId);
    Task<BookingModel> CancelBookingAsync(Guid bookingId, Guid userId, bool isAdmin);
    Task<BookingModel?> GetBookingByIdAsync(Guid bookingId, Guid? userId = null, bool isAdmin = false);
    Task<IReadOnlyList<BookingModel>> GetPendingAsync();
    Task UpdateAsync(BookingModel booking);
}
