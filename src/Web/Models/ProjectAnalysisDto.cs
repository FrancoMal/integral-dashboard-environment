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
    public string UserNotes { get; set; } = string.Empty;
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

public class ProjectFeatureDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public string FilesToModify { get; set; } = string.Empty;
    public string Complexity { get; set; } = "media";
    public string UserNotes { get; set; } = string.Empty;
    public bool AddedToBacklog { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AnalyzeFeaturesResultDto
{
    public int Generated { get; set; }
    public int TotalFeatures { get; set; }
}

public class OrchestratorStatusDto
{
    public string Status { get; set; } = "offline";
    public string Provider { get; set; } = string.Empty;
    public int? CurrentProjectId { get; set; }
    public string CurrentProjectName { get; set; } = string.Empty;
    public string CurrentTask { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? LastHeartbeat { get; set; }
    public bool IsAlive { get; set; }
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
