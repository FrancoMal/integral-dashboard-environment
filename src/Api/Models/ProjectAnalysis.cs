namespace Api.Models;

public class ProjectAnalysis
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string Summary { get; set; } = string.Empty;
    public int RecommendationsGenerated { get; set; }
    public int TotalRecommendations { get; set; }
    public string DetectedStack { get; set; } = string.Empty;
    public string DetectedTools { get; set; } = string.Empty;
    public int FilesAnalyzed { get; set; }
}
