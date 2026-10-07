using System.Text.Json.Serialization;
using Domain.Exceptions;
using Domain.Models.Enums;

namespace Domain.Models;

public class BookingModel
{
    private BookingModel()
    {
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public int EventId { get; private set; }
    public BookingStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    [JsonIgnore]
    public EventModel Event { get; private set; } = null!;
    [JsonIgnore]
    public UserModel User { get; private set; } = null!;

    public static BookingModel Create(int eventId, Guid userId)
    {
        return new BookingModel
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            UserId = userId,
            Status = BookingStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            ProcessedAt = null
        };
    }

    public void Confirm()
    {
        Status = BookingStatus.Confirmed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Reject()
    {
        Status = BookingStatus.Rejected;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status == BookingStatus.Cancelled)
            throw new CancelledValidationException("Бронирование уже отменено.");

        if (Status == BookingStatus.Rejected)
            throw new CancelledValidationException("Отклонённое бронирование нельзя отменить.");

        Status = BookingStatus.Cancelled;
        ProcessedAt = DateTime.UtcNow;
    }
}
