using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using EventManager.Models.Enums;

namespace EventManager.Models;

public class BookingModel : IValidatableObject
{
    private BookingModel()
    {
    }

    [Required]
    public Guid Id { get; set; }
    [Required]
    public int EventId { get; set; }
    [Required]
    public BookingStatus Status { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

    [JsonIgnore]
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
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ProcessedAt <= CreatedAt)
        {
            yield return new ValidationResult(
                "Дата обработки не может быть раньше или равна дате начала",
                new[] { nameof(ProcessedAt) }
            );
        }
    }
}