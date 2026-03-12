namespace Api.Models;

public class OrchestratorState
{
    public int Id { get; set; }
    public string Status { get; set; } = "offline";
    public string Provider { get; set; } = string.Empty;
    public int? CurrentProjectId { get; set; }
    public string CurrentProjectName { get; set; } = string.Empty;
    public string CurrentTask { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
}
