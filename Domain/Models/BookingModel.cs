using Domain.Models.Enums;

namespace Domain.Models;

public class BookingModel 
{
    private BookingModel()
    {
    }


    public Guid Id { get; set; }

    public int EventId { get; set; }
    
    public BookingStatus Status { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

   
    public EventModel Event { get; set; } = null!;

    public static BookingModel Create(int eventId)
    {
        return new BookingModel
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
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
}