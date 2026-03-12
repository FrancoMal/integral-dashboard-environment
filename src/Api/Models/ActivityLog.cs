namespace Api.Models;

public class ActivityLog
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Status { get; set; } = "done";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
