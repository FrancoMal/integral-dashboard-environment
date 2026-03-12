using Api.Data;
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
}
