namespace Web.Models;

public class GitHubRepoDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Language { get; set; }
    public bool IsPrivate { get; set; }
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsImported { get; set; }
}
