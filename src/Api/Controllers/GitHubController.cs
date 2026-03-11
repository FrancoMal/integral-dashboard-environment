using Api.Data;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GitHubController : ControllerBase
{
    private readonly GitHubService _github;
    private readonly AppDbContext _db;

    public GitHubController(GitHubService github, AppDbContext db)
    {
        _github = github;
        _db = db;
    }

    /// <summary>
    /// Lista los repos del usuario autenticado en GitHub.
    /// Incluye flag indicando si ya fue importado a Proyectos.
    /// </summary>
    [HttpGet("repos")]
    public async Task<IActionResult> GetRepos()
    {
        var repos = await _github.GetUserReposAsync();
        var importedIds = await _db.Projects
            .Where(p => p.IsActive)
            .Select(p => p.GitHubRepoId)
            .ToListAsync();

        var result = repos.Select(r => new
        {
            r.Id,
            r.Name,
            r.FullName,
            r.Description,
            r.HtmlUrl,
            r.Language,
            isPrivate = r.Private,
            r.StargazersCount,
            r.ForksCount,
            r.UpdatedAt,
            r.CreatedAt,
            isImported = importedIds.Contains(r.Id)
        });

        return Ok(result);
    }

    /// <summary>
    /// Importa un repo de GitHub como Proyecto activo.
    /// </summary>
    [HttpPost("import")]
    public async Task<IActionResult> ImportRepo([FromBody] ImportRepoRequest request)
    {
        var existing = await _db.Projects
            .FirstOrDefaultAsync(p => p.GitHubRepoId == request.RepoId);

        if (existing != null)
        {
            if (existing.IsActive)
                return Conflict(new { message = "Este repo ya esta importado." });

            // Reactivar proyecto previamente eliminado
            existing.IsActive = true;
            existing.ImportedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(existing);
        }

        var repo = await _github.GetUserReposAsync();
        var target = repo.FirstOrDefault(r => r.Id == request.RepoId);
        if (target == null)
            return NotFound(new { message = "Repo no encontrado en GitHub." });

        var project = new Project
        {
            GitHubRepoId = target.Id,
            Name = target.Name,
            FullName = target.FullName,
            Description = target.Description,
            HtmlUrl = target.HtmlUrl,
            Language = target.Language,
            IsPrivate = target.Private
        };

        _db.Projects.Add(project);
        await _db.SaveChangesAsync();

        return Ok(project);
    }

    /// <summary>
    /// Devuelve los proyectos activos (importados).
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _db.Projects
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.ImportedAt)
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>
    /// Elimina (desactiva) un proyecto importado.
    /// </summary>
    [HttpDelete("projects/{id}")]
    public async Task<IActionResult> RemoveProject(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null)
            return NotFound();

        project.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Devuelve el usuario de GitHub autenticado.
    /// </summary>
    [HttpGet("user")]
    public async Task<IActionResult> GetGitHubUser()
    {
        var login = await _github.GetAuthenticatedUserAsync();
        if (login == null)
            return BadRequest(new { message = "No se pudo autenticar con GitHub. Verifica el token." });

        return Ok(new { login });
    }
}

public class ImportRepoRequest
{
    public long RepoId { get; set; }
}
