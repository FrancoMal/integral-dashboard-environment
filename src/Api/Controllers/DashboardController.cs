using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalUsers = await _db.Users.CountAsync(u => u.IsActive);
        var totalProjects = await _db.Projects.CountAsync(p => p.IsActive);
        var totalAnalyses = await _db.ProjectAnalyses.CountAsync();
        var totalRecommendations = await _db.ProjectRecommendations.CountAsync();
        var totalFeatures = await _db.ProjectFeatures.CountAsync();
        var totalWorkItems = await _db.ProjectWorkItems.CountAsync();

        return Ok(new
        {
            totalUsers,
            totalProjects,
            totalAnalyses,
            totalRecommendations,
            totalFeatures,
            totalWorkItems,
            systemStatus = "Online"
        });
    }

    [HttpGet("activities")]
    public async Task<IActionResult> GetActivities([FromQuery] string? source = null, [FromQuery] int? projectId = null)
    {
        var query = _db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrEmpty(source))
            query = query.Where(a => a.Source == source);

        if (projectId.HasValue)
            query = query.Where(a => a.ProjectId == projectId.Value);

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .Select(a => new
            {
                a.Id,
                a.ProjectId,
                a.ProjectName,
                a.Action,
                title = a.Title,
                description = a.Detail,
                status = a.Status,
                source = a.Source,
                timestamp = a.CreatedAt
            })
            .ToListAsync();

        var sourceCounts = await _db.ActivityLogs
            .GroupBy(a => a.Source)
            .Select(g => new { source = g.Key, count = g.Count() })
            .ToListAsync();

        return Ok(new
        {
            total = items.Count,
            inProgress = items.Count(x => x.status == "in_progress" || x.status == "running"),
            done = items.Count(x => x.status == "done"),
            sources = sourceCounts,
            items
        });
    }

    [HttpGet("orchestrator/status")]
    public async Task<IActionResult> GetOrchestratorStatus()
    {
        var state = await _db.OrchestratorStates.OrderByDescending(s => s.Id).FirstOrDefaultAsync();
        if (state == null)
            return Ok(new { status = "offline", provider = "", currentTask = "", output = "", lastHeartbeat = (DateTime?)null });

        // If no heartbeat in 90s, consider offline
        var isAlive = (DateTime.UtcNow - state.LastHeartbeat).TotalSeconds < 90;

        return Ok(new
        {
            status = isAlive ? state.Status : "offline",
            state.Provider,
            state.CurrentProjectId,
            state.CurrentProjectName,
            state.CurrentTask,
            output = state.Output,
            state.StartedAt,
            state.LastHeartbeat,
            isAlive
        });
    }

    [HttpPost("orchestrator/heartbeat")]
    public async Task<IActionResult> OrchestratorHeartbeat([FromBody] OrchestratorHeartbeatRequest request)
    {
        var state = await _db.OrchestratorStates.OrderByDescending(s => s.Id).FirstOrDefaultAsync();

        if (state == null)
        {
            state = new OrchestratorState();
            _db.OrchestratorStates.Add(state);
        }

        state.Status = request.Status ?? "idle";
        state.Provider = request.Provider ?? "";
        state.CurrentProjectId = request.CurrentProjectId;
        state.CurrentProjectName = request.CurrentProjectName ?? "";
        state.CurrentTask = request.CurrentTask ?? "";
        state.LastHeartbeat = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(request.OutputAppend))
        {
            // Keep last 10KB of output
            state.Output = (state.Output + request.OutputAppend).Length > 10000
                ? (state.Output + request.OutputAppend)[^10000..]
                : state.Output + request.OutputAppend;
        }

        if (request.ClearOutput == true)
            state.Output = "";

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("activity-log")]
    public async Task<IActionResult> CreateActivityLog([FromBody] ActivityLogRequest request)
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = request.ProjectId,
            ProjectName = request.ProjectName ?? "",
            Action = request.Action ?? "unknown",
            Title = request.Title ?? "",
            Detail = request.Detail ?? "",
            Source = request.Source ?? "sistema",
            Status = request.Status ?? "done"
        });

        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class OrchestratorHeartbeatRequest
{
    public string? Status { get; set; }
    public string? Provider { get; set; }
    public int? CurrentProjectId { get; set; }
    public string? CurrentProjectName { get; set; }
    public string? CurrentTask { get; set; }
    public string? OutputAppend { get; set; }
    public bool? ClearOutput { get; set; }
}

public class ActivityLogRequest
{
    public int? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? Action { get; set; }
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public string? Source { get; set; }
    public string? Status { get; set; }
}
