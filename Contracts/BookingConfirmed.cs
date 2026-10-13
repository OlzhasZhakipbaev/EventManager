namespace Contracts;

public sealed record BookingConfirmed(
    Guid BookingId,
    int EventId,
    Guid UserId,
    int SeatCount,
    DateTime ConfirmedAt)
{
    public const string Topic = "booking-confirmed";
}
