namespace Api.Models;

public class ProjectFeature
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public string FilesToModify { get; set; } = string.Empty;
    public string Complexity { get; set; } = "media";
    public string UserNotes { get; set; } = string.Empty;
    public bool AddedToBacklog { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
