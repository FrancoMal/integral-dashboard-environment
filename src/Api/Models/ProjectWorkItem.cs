namespace Api.Models;

public class ProjectWorkItem
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = "backlog";
    public string Source { get; set; } = "manual";
    public string ErrorMessage { get; set; } = string.Empty;
    public DateTime? ErrorAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
