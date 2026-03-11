namespace Web.Models;

public class ProjectRecommendationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int Priority { get; set; }
    public bool Selected { get; set; }
    public bool AddedToBacklog { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProjectWorkItemDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AnalyzeProjectResultDto
{
    public int Generated { get; set; }
    public int TotalRecommendations { get; set; }
    public int AnalysisId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string DetectedStack { get; set; } = string.Empty;
    public string DetectedTools { get; set; } = string.Empty;
    public int FilesAnalyzed { get; set; }
}

public class ProjectAnalysisEntryDto
{
    public int Id { get; set; }
    public DateTime AnalyzedAt { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int RecommendationsGenerated { get; set; }
    public int TotalRecommendations { get; set; }
    public string DetectedStack { get; set; } = string.Empty;
    public string DetectedTools { get; set; } = string.Empty;
    public int FilesAnalyzed { get; set; }
}
