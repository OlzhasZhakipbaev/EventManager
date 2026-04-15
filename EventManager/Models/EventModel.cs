using System.ComponentModel.DataAnnotations;

namespace EventManager.Models;

public class EventModel
{
    [Required]
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }
}