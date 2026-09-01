using System.ComponentModel.DataAnnotations;
using EventManager.Models.Enums;

namespace EventManager.Models;

public class BookingModel : IValidatableObject
{
    [Required]
    public Guid Id { get; set; }
    [Required]
    public int EventId { get; set; }
    [Required]
    public BookingStatus Status { get; set; }
    [Required]
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }

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