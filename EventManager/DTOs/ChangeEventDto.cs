namespace EventManager.DTOs;

public class ChangeEventDto
{
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public DateTime StartAt { get; set; }
    
    public DateTime EndAt { get; set; }
}