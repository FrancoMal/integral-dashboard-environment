namespace Web.Models;

public class ProjectDto
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
    public bool IsActive { get; set; }
}
