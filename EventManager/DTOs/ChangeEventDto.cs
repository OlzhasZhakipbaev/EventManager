using System.ComponentModel.DataAnnotations;

namespace EventManager.DTOs;

public class ChangeEventDto : IValidatableObject
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public DateTime StartAt { get; set; }
    
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

