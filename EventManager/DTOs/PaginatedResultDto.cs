namespace EventManager.DTOs;

public class PaginatedResultDto<T>
{
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public List<T> EventList { get; set; }
}