using Contracts;

namespace Bookings.Application.Messaging;

public interface IBookingConfirmedPublisher
{
    Task PublishAsync(BookingConfirmed message, CancellationToken cancellationToken = default);
}
