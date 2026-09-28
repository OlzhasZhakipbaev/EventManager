using Domain.Models;

namespace Application.Services.Booking;

public interface IBookingService
{
    Task ProcessPendingAsync(CancellationToken ct);
    Task<BookingModel> CreateBookingAsync(int eventId);
    Task<BookingModel?> GetBookingByIdAsync(Guid bookingId);
    Task<IReadOnlyList<BookingModel>> GetPendingAsync();
    Task UpdateAsync(BookingModel booking);
}
