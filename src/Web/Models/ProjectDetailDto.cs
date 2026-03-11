namespace Web.Models;

public class ProjectDetailDto
{
    public ProjectDetailProject Project { get; set; } = new();
    public ProjectDetailRepo? Repo { get; set; }
    public List<ProjectDetailCommit> Commits { get; set; } = new();
    public List<ProjectRecommendationDto> Recommendations { get; set; } = new();
    public List<ProjectWorkItemDto> WorkItems { get; set; } = new();
    public List<ProjectAnalysisEntryDto> Analyses { get; set; } = new();
    public string? Readme { get; set; }
}

public class ProjectDetailProject
{
    public int Id { get; set; }
    public long GitHubRepoId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Language { get; set; }
    public bool IsPrivate { get; set; }
    public DateTime ImportedAt { get; set; }
}

public class ProjectDetailRepo
{
    public string DefaultBranch { get; set; } = "main";
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public int OpenIssuesCount { get; set; }
    public DateTime PushedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ProjectDetailCommit
{
    public string Sha { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? AuthorLogin { get; set; }
    public string? AuthorAvatar { get; set; }
}
