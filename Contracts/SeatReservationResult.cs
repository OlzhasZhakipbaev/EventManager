namespace Contracts;

public sealed class SeatReservationResult
{
    public bool Success { get; set; }
    public int AvailableSeats { get; set; }
}
