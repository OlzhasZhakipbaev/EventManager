namespace Domain.Exceptions;

public class EventAlreadyPassedException : DomainValidationException
{
    public int EventId { get; }
    public DateTime EventDate { get; }

    public EventAlreadyPassedException(int eventId, DateTime eventDate)
        : base($"Событие {eventId} состоялось {eventDate:dd.MM.yyyy HH:mm}, забронировать его нельзя.")
    {
        EventId = eventId;
        EventDate = eventDate;
    }
}
