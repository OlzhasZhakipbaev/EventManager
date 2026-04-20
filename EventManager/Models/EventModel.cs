using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

public class EventModel : IValidatableObject
{
    [Required]
    public int Id { get; init; }

    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
    
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EndAt <= StartAt)
        {
            yield return new ValidationResult(
                "Дата окончания не может быть раньше или равна дате начала",
                new[] { nameof(EndAt) }
            );
        }
    }
}