namespace Bookings.Domain.Exceptions;

public sealed class EventAlreadyPassedException : DomainValidationException
{
    public int EventId { get; }
    public DateTime StartAt { get; }

    public EventAlreadyPassedException(int eventId, DateTime startAt)
        : base($"Событие {eventId} уже началось ({startAt:O}).")
    {
        EventId = eventId;
        StartAt = startAt;
    }
}
