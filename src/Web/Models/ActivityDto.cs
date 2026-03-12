namespace Web.Models;

public class ActivityDto
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ActivitySourceCount
{
    public string Source { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ActivitySummaryDto
{
    public int Total { get; set; }
    public int InProgress { get; set; }
    public int Done { get; set; }
    public List<ActivitySourceCount> Sources { get; set; } = new();
    public List<ActivityDto> Items { get; set; } = new();
}
