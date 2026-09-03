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

    [Required]
    public int TotalSeats { get; set; }

    public int AvailableSeats { get; set; }

    public static EventModel Create(
        int id,
        string title,
        string? description,
        DateTime startAt,
        DateTime endAt,
        int totalSeats)
    {
        if (totalSeats <= 0)
            throw new ValidationException("Количество мест должно быть больше нуля");

        return new EventModel
        {
            Id = id,
            Title = title,
            Description = description,
            StartAt = startAt,
            EndAt = endAt,
            TotalSeats = totalSeats,
            AvailableSeats = totalSeats
        };
    }

    public bool TryReserveSeats(int count = 1)
    {
        if (count <= 0 || AvailableSeats < count)
            return false;

        AvailableSeats -= count;
        return true;
    }

    public void ReleaseSeats(int count = 1)
    {
        if (count <= 0)
            return;

        AvailableSeats += count;
        if (AvailableSeats > TotalSeats)
            AvailableSeats = TotalSeats;
    }
    
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