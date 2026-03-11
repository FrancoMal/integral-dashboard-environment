namespace Api.Models;

public class ProjectRecommendation
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public int Priority { get; set; } = 2;
    public bool Selected { get; set; }
    public bool AddedToBacklog { get; set; }
    public string UserNotes { get; set; } = string.Empty;
    public int? AnalysisId { get; set; }
    public ProjectAnalysis? Analysis { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
