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
    private readonly ClaudeAnalyzerService _claude;

    public GitHubController(GitHubService github, AppDbContext db, ClaudeAnalyzerService claude)
    {
        _github = github;
        _db = db;
        _claude = claude;
    }

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

    [HttpPost("import")]
    public async Task<IActionResult> ImportRepo([FromBody] ImportRepoRequest request)
    {
        var existing = await _db.Projects
            .FirstOrDefaultAsync(p => p.GitHubRepoId == request.RepoId);

        if (existing != null)
        {
            if (existing.IsActive)
                return Conflict(new { message = "Este repo ya esta importado." });

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
        Log("import", $"Proyecto importado: {project.Name}", "github",
            $"Repo {project.FullName} importado desde GitHub", projectId: null, projectName: project.Name);
        await _db.SaveChangesAsync();

        return Ok(project);
    }

    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var projects = await _db.Projects
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.ImportedAt)
            .ToListAsync();

        return Ok(projects);
    }

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

    [HttpGet("projects/{id}/detail")]
    public async Task<IActionResult> GetProjectDetail(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null || !project.IsActive)
            return NotFound();

        var repoDetail = await _github.GetRepoDetailAsync(project.FullName);
        var commits = await _github.GetRepoCommitsAsync(project.FullName, 20);
        var readme = await _github.GetRepoReadmeAsync(project.FullName);
        var recommendations = await _db.ProjectRecommendations
            .Where(r => r.ProjectId == project.Id)
            .OrderByDescending(r => r.Priority)
            .ThenByDescending(r => r.CreatedAt)
            .ToListAsync();
        var workItems = await _db.ProjectWorkItems
            .Where(w => w.ProjectId == project.Id)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
        var analyses = await _db.ProjectAnalyses
            .Where(a => a.ProjectId == project.Id)
            .OrderByDescending(a => a.AnalyzedAt)
            .ToListAsync();
        var features = await _db.ProjectFeatures
            .Where(f => f.ProjectId == project.Id)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return Ok(new
        {
            project = new
            {
                project.Id,
                project.GitHubRepoId,
                project.Name,
                project.FullName,
                project.Description,
                project.HtmlUrl,
                project.Language,
                project.IsPrivate,
                project.ImportedAt
            },
            repo = repoDetail != null ? new
            {
                repoDetail.DefaultBranch,
                repoDetail.StargazersCount,
                repoDetail.ForksCount,
                repoDetail.OpenIssuesCount,
                repoDetail.PushedAt,
                repoDetail.CreatedAt,
                repoDetail.UpdatedAt
            } : null,
            commits = commits.Select(c => new
            {
                sha = c.Sha,
                htmlUrl = c.HtmlUrl,
                message = c.Commit.Message,
                authorName = c.Commit.Author.Name,
                date = c.Commit.Author.Date,
                authorLogin = c.Author?.Login,
                authorAvatar = c.Author?.AvatarUrl
            }),
            recommendations = recommendations.Select(r => new
            {
                r.Id,
                r.Title,
                r.Notes,
                r.Category,
                r.Priority,
                r.Selected,
                r.AddedToBacklog,
                r.UserNotes,
                r.CreatedAt
            }),
            workItems = workItems.Select(w => new
            {
                w.Id,
                w.Title,
                w.Notes,
                w.Status,
                w.Source,
                w.CreatedAt
            }),
            readme,
            features = features.Select(f => new
            {
                f.Id,
                f.Title,
                f.Description,
                f.Implementation,
                f.FilesToModify,
                f.Complexity,
                f.UserNotes,
                f.AddedToBacklog,
                f.CreatedAt
            }),
            analyses = analyses.Select(a => new
            {
                a.Id,
                a.AnalyzedAt,
                a.Summary,
                a.RecommendationsGenerated,
                a.TotalRecommendations,
                a.DetectedStack,
                a.DetectedTools,
                a.FilesAnalyzed
            })
        });
    }

    [HttpPost("projects/{id}/analyze")]
    public async Task<IActionResult> AnalyzeProject(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null || !project.IsActive)
            return NotFound();

        var repoDetail = await _github.GetRepoDetailAsync(project.FullName);
        var commits = await _github.GetRepoCommitsAsync(project.FullName, 10);
        var readme = await _github.GetRepoReadmeAsync(project.FullName) ?? string.Empty;
        var defaultBranch = repoDetail?.DefaultBranch ?? "main";
        var tree = await _github.GetRepoTreeAsync(project.FullName, defaultBranch);
        var languages = await _github.GetRepoLanguagesAsync(project.FullName);

        var filePaths = tree.Where(t => t.Type == "blob").Select(t => t.Path).ToList();
        var dirPaths = tree.Where(t => t.Type == "tree").Select(t => t.Path).ToHashSet();

        // Deteccion de stack y herramientas
        var stackItems = new List<string>();
        var toolItems = new List<string>();
        var summaryParts = new List<string>();

        // Lenguajes
        if (languages.Count > 0)
        {
            var totalBytes = languages.Sum(l => l.Bytes);
            var topLangs = languages.Take(4).Select(l =>
            {
                var pct = totalBytes > 0 ? (l.Bytes * 100.0 / totalBytes).ToString("F0") : "0";
                return $"{l.Name} ({pct}%)";
            });
            stackItems.AddRange(languages.Take(4).Select(l => l.Name));
            summaryParts.Add($"Lenguajes: {string.Join(", ", topLangs)}");
        }

        // Deteccion de frameworks y package managers
        bool hasPackageJson = filePaths.Any(f => f == "package.json");
        bool hasCsproj = filePaths.Any(f => f.EndsWith(".csproj"));
        bool hasCargoToml = filePaths.Any(f => f == "Cargo.toml");
        bool hasGoMod = filePaths.Any(f => f == "go.mod");
        bool hasRequirementsTxt = filePaths.Any(f => f == "requirements.txt");
        bool hasPyprojectToml = filePaths.Any(f => f == "pyproject.toml");
        bool hasComposerJson = filePaths.Any(f => f == "composer.json");
        bool hasGemfile = filePaths.Any(f => f == "Gemfile");

        if (hasPackageJson) toolItems.Add("npm/Node.js");
        if (hasCsproj) toolItems.Add(".NET/C#");
        if (hasCargoToml) toolItems.Add("Cargo/Rust");
        if (hasGoMod) toolItems.Add("Go modules");
        if (hasRequirementsTxt || hasPyprojectToml) toolItems.Add("Python packages");
        if (hasComposerJson) toolItems.Add("Composer/PHP");
        if (hasGemfile) toolItems.Add("Bundler/Ruby");

        // CI/CD
        bool hasGitHubActions = filePaths.Any(f => f.StartsWith(".github/workflows/"));
        bool hasGitLabCI = filePaths.Any(f => f == ".gitlab-ci.yml");
        bool hasJenkinsfile = filePaths.Any(f => f == "Jenkinsfile");
        bool hasCircleCI = filePaths.Any(f => f.StartsWith(".circleci/"));
        bool hasAnyCI = hasGitHubActions || hasGitLabCI || hasJenkinsfile || hasCircleCI;

        if (hasGitHubActions) toolItems.Add("GitHub Actions");
        if (hasGitLabCI) toolItems.Add("GitLab CI");
        if (hasJenkinsfile) toolItems.Add("Jenkins");

        // Docker
        bool hasDockerfile = filePaths.Any(f => f == "Dockerfile" || f.EndsWith("/Dockerfile"));
        bool hasDockerCompose = filePaths.Any(f => f.StartsWith("docker-compose") || f.StartsWith("compose."));

        if (hasDockerfile) toolItems.Add("Docker");
        if (hasDockerCompose) toolItems.Add("Docker Compose");

        // Tests
        bool hasTestDir = dirPaths.Any(d => d.Contains("test", StringComparison.OrdinalIgnoreCase) ||
                                             d.Contains("spec", StringComparison.OrdinalIgnoreCase) ||
                                             d.Contains("__tests__", StringComparison.OrdinalIgnoreCase));
        bool hasTestFiles = filePaths.Any(f => f.Contains(".test.", StringComparison.OrdinalIgnoreCase) ||
                                               f.Contains(".spec.", StringComparison.OrdinalIgnoreCase) ||
                                               f.Contains("Test.cs", StringComparison.OrdinalIgnoreCase));
        bool hasAnyTests = hasTestDir || hasTestFiles;

        // Otros archivos clave
        bool hasLicense = filePaths.Any(f => f.StartsWith("LICENSE", StringComparison.OrdinalIgnoreCase));
        bool hasGitignore = filePaths.Any(f => f == ".gitignore");
        bool hasEditorConfig = filePaths.Any(f => f == ".editorconfig");
        bool hasLintConfig = filePaths.Any(f => f == ".eslintrc.json" || f == ".eslintrc.js" || f == ".eslintrc" ||
                                                 f == ".prettierrc" || f == ".prettierrc.json" || f == "biome.json");
        bool hasEnvExample = filePaths.Any(f => f == ".env.example" || f == ".env.sample");
        bool hasContributing = filePaths.Any(f => f.StartsWith("CONTRIBUTING", StringComparison.OrdinalIgnoreCase));
        bool hasChangelog = filePaths.Any(f => f.StartsWith("CHANGELOG", StringComparison.OrdinalIgnoreCase));

        int fileCount = filePaths.Count;
        summaryParts.Add($"{fileCount} archivos en el repositorio");
        if (toolItems.Count > 0) summaryParts.Add($"Herramientas: {string.Join(", ", toolItems)}");
        if (hasAnyTests) summaryParts.Add("Tests detectados");
        if (hasAnyCI) summaryParts.Add("CI/CD configurado");
        if (hasDockerfile) summaryParts.Add("Docker presente");

        // Generar recomendaciones profundas
        var candidates = new List<ProjectRecommendationSeed>();

        // ---- Documentacion ----
        if (string.IsNullOrWhiteSpace(readme))
        {
            candidates.Add(new("Crear README con setup y contexto del proyecto",
                "No se encontro README. Es critico para que cualquiera (incluido Jeff) pueda entender y levantar el proyecto rapido. Incluir: que hace, como instalarlo, como correrlo.",
                "documentacion", 3));
        }
        else if (readme.Length < 400)
        {
            candidates.Add(new("Expandir README del proyecto",
                "El README existe pero es muy breve. Sumar seccion de arquitectura, setup local, variables de entorno y pasos de despliegue.",
                "documentacion", 2));
        }

        if (!hasLicense)
        {
            candidates.Add(new("Agregar licencia al repositorio",
                "No se detecto archivo LICENSE. Definir la licencia aclara los terminos de uso y contribucion.",
                "documentacion", 1));
        }

        if (!hasContributing && fileCount > 30)
        {
            candidates.Add(new("Crear guia de contribucion",
                "El proyecto tiene mas de 30 archivos pero no tiene CONTRIBUTING.md. Tener guia de contribucion ayuda a mantener consistencia.",
                "documentacion", 1));
        }

        if (!hasChangelog && commits.Count > 5)
        {
            candidates.Add(new("Agregar CHANGELOG para registrar cambios",
                "No se detecto CHANGELOG. Mantener un registro de cambios importantes facilita el seguimiento de versiones.",
                "documentacion", 1));
        }

        // ---- CI/CD ----
        if (!hasAnyCI)
        {
            if (hasPackageJson)
                candidates.Add(new("Configurar CI con GitHub Actions para Node.js",
                    "El proyecto usa Node.js pero no tiene CI/CD. Un workflow basico con install + lint + test evitaria errores en cada push.",
                    "devops", 3));
            else if (hasCsproj)
                candidates.Add(new("Configurar CI con GitHub Actions para .NET",
                    "El proyecto usa .NET pero no tiene pipeline de CI. Un workflow con dotnet build + test atraparia errores automaticamente.",
                    "devops", 3));
            else
                candidates.Add(new("Configurar pipeline de CI/CD basico",
                    "No se detecto ningun pipeline de integracion continua. Agregar uno reduce el riesgo de subir codigo roto.",
                    "devops", 2));
        }

        // ---- Tests ----
        if (!hasAnyTests)
        {
            candidates.Add(new("Agregar suite de tests basica",
                $"No se detectaron tests en el repositorio ({fileCount} archivos analizados). Empezar con tests para las funciones mas criticas del proyecto.",
                "calidad", 3));
        }
        else
        {
            var testFileCount = filePaths.Count(f => f.Contains(".test.", StringComparison.OrdinalIgnoreCase) ||
                                                     f.Contains(".spec.", StringComparison.OrdinalIgnoreCase) ||
                                                     f.Contains("Test.cs", StringComparison.OrdinalIgnoreCase));
            if (testFileCount < 3 && fileCount > 20)
            {
                candidates.Add(new("Ampliar cobertura de tests",
                    $"Se detectaron solo {testFileCount} archivos de test para un proyecto con {fileCount} archivos. Aumentar la cobertura mejoraria la confianza en los cambios.",
                    "calidad", 2));
            }
        }

        // ---- Docker ----
        if (!hasDockerfile && fileCount > 10)
        {
            candidates.Add(new("Agregar Dockerfile para estandarizar el entorno",
                "No se detecto Dockerfile. Dockerizar el proyecto permite que cualquiera lo levante de forma identica sin problemas de entorno.",
                "devops", 2));
        }

        if (hasDockerfile && !hasDockerCompose && fileCount > 15)
        {
            candidates.Add(new("Agregar docker-compose para orquestar servicios",
                "Hay Dockerfile pero no docker-compose. Si el proyecto necesita base de datos u otros servicios, compose simplifica mucho el desarrollo local.",
                "devops", 1));
        }

        // ---- Calidad de codigo ----
        if (!hasGitignore)
        {
            candidates.Add(new("Agregar .gitignore",
                "No se detecto .gitignore. Sin el, pueden subirse archivos innecesarios (binarios, node_modules, etc) al repositorio.",
                "calidad", 3));
        }

        if (!hasLintConfig && (hasPackageJson || filePaths.Any(f => f.EndsWith(".ts") || f.EndsWith(".js"))))
        {
            candidates.Add(new("Configurar linter para el proyecto",
                "Se detecta codigo JavaScript/TypeScript pero no hay configuracion de linter (ESLint, Prettier, Biome). Un linter ayuda a mantener estilo consistente.",
                "calidad", 2));
        }

        if (!hasEditorConfig && fileCount > 10)
        {
            candidates.Add(new("Agregar .editorconfig para consistencia de formato",
                "No se detecto .editorconfig. Este archivo estandariza tabs/espacios, encoding y saltos de linea entre editores.",
                "calidad", 1));
        }

        if (!hasEnvExample && filePaths.Any(f => f == ".env"))
        {
            candidates.Add(new("Crear .env.example como referencia",
                "Se detecto un .env pero no un .env.example. Tener un ejemplo documentado evita que alguien nuevo no sepa que variables configurar.",
                "documentacion", 2));
        }

        // ---- Planning / actividad ----
        if (repoDetail?.OpenIssuesCount > 0)
        {
            candidates.Add(new("Revisar issues abiertas y priorizar backlog",
                $"Hay {repoDetail.OpenIssuesCount} issues abiertas. Revisar y priorizar las mas relevantes como tareas dentro del dashboard.",
                "planning", 2));
        }

        if (commits.Count <= 2)
        {
            candidates.Add(new("Definir primer plan de trabajo",
                "Hay muy poca actividad en el repo. Un backlog inicial con 2-3 entregables visibles puede arrancar el proyecto.",
                "planning", 3));
        }
        else
        {
            var lastCommitDate = commits.FirstOrDefault()?.Commit?.Author?.Date;
            if (lastCommitDate.HasValue)
            {
                var daysSinceLastCommit = (DateTime.UtcNow - lastCommitDate.Value).TotalDays;
                if (daysSinceLastCommit > 30)
                {
                    candidates.Add(new("Retomar actividad del proyecto",
                        $"El ultimo commit fue hace {(int)daysSinceLastCommit} dias. Revisar si hay trabajo pendiente y retomar con un plan claro.",
                        "seguimiento", 2));
                }
            }
        }

        // ---- Seguridad basica ----
        if (filePaths.Any(f => f == ".env") && !filePaths.Any(f => f == ".gitignore"))
        {
            candidates.Add(new("Evitar exponer credenciales en el repositorio",
                "Se detecto un archivo .env sin .gitignore. Existe riesgo de exponer API keys o passwords. Crear .gitignore y excluir .env de inmediato.",
                "seguridad", 3));
        }

        // ---- Analisis profundo con Claude AI ----
        bool claudeUsed = false;
        if (await _claude.IsAvailableAsync())
        {
            try
            {
                // Seleccionar archivos clave para revision de codigo
                var keyFiles = SelectKeyFiles(filePaths, project.Language);
                var codeFiles = new List<CodeFile>();

                foreach (var filePath in keyFiles.Take(12))
                {
                    var content = await _github.GetFileContentAsync(project.FullName, filePath);
                    if (!string.IsNullOrEmpty(content))
                        codeFiles.Add(new CodeFile { Path = filePath, Content = content });
                }

                if (codeFiles.Count > 0)
                {
                    var claudeRecs = await _claude.AnalyzeCodeAsync(
                        project.FullName, project.Language, codeFiles);

                    foreach (var cr in claudeRecs)
                    {
                        candidates.Add(new(
                            cr.Title,
                            cr.Notes,
                            cr.Category,
                            Math.Clamp(cr.Priority, 1, 3)));
                    }

                    claudeUsed = true;
                    summaryParts.Add($"Analisis IA sobre {codeFiles.Count} archivos clave");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AnalyzeProject] Claude analysis error: {ex.Message}");
            }
        }

        // Guardar el analisis en historial
        var analysis = new ProjectAnalysis
        {
            ProjectId = project.Id,
            Summary = string.Join(". ", summaryParts),
            DetectedStack = string.Join(", ", stackItems),
            DetectedTools = string.Join(", ", toolItems),
            FilesAnalyzed = fileCount
        };

        int generated = 0;

        foreach (var candidate in candidates)
        {
            var exists = await _db.ProjectRecommendations.AnyAsync(r => r.ProjectId == project.Id && r.Title == candidate.Title);
            if (exists) continue;

            _db.ProjectRecommendations.Add(new ProjectRecommendation
            {
                ProjectId = project.Id,
                Title = candidate.Title,
                Notes = candidate.Notes,
                Category = candidate.Category,
                Priority = candidate.Priority
            });
            generated++;
        }

        analysis.RecommendationsGenerated = generated;
        analysis.TotalRecommendations = await _db.ProjectRecommendations.CountAsync(r => r.ProjectId == project.Id) + generated;
        _db.ProjectAnalyses.Add(analysis);

        var analysisSummary = claudeUsed ? "Analisis profundo con IA" : "Analisis estructural";
        Log("analyze", $"{analysisSummary}: {generated} recomendaciones nuevas", "analisis",
            $"{analysis.Summary}. {fileCount} archivos, stack: {analysis.DetectedStack}",
            projectId: project.Id, projectName: project.Name);

        await _db.SaveChangesAsync();

        // Vincular recomendaciones nuevas al analisis
        var unlinked = await _db.ProjectRecommendations
            .Where(r => r.ProjectId == project.Id && r.AnalysisId == null)
            .ToListAsync();
        foreach (var rec in unlinked)
            rec.AnalysisId = analysis.Id;
        await _db.SaveChangesAsync();

        var total = await _db.ProjectRecommendations.CountAsync(r => r.ProjectId == project.Id);
        return Ok(new
        {
            generated,
            totalRecommendations = total,
            analysisId = analysis.Id,
            summary = analysis.Summary,
            detectedStack = analysis.DetectedStack,
            detectedTools = analysis.DetectedTools,
            filesAnalyzed = analysis.FilesAnalyzed,
            claudeAnalysis = claudeUsed
        });
    }

    [HttpPost("projects/{id}/features")]
    public async Task<IActionResult> AnalyzeFeatures(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null || !project.IsActive)
            return NotFound();

        if (!await _claude.IsAvailableAsync())
            return StatusCode(503, new { message = "El servicio de analisis IA no esta disponible." });

        var repoDetail = await _github.GetRepoDetailAsync(project.FullName);
        var defaultBranch = repoDetail?.DefaultBranch ?? "main";
        var tree = await _github.GetRepoTreeAsync(project.FullName, defaultBranch);
        var readme = await _github.GetRepoReadmeAsync(project.FullName);

        var filePaths = tree.Where(t => t.Type == "blob").Select(t => t.Path).ToList();
        var keyFiles = SelectKeyFiles(filePaths, project.Language);
        var codeFiles = new List<CodeFile>();

        foreach (var filePath in keyFiles.Take(10))
        {
            var content = await _github.GetFileContentAsync(project.FullName, filePath);
            if (!string.IsNullOrEmpty(content))
                codeFiles.Add(new CodeFile { Path = filePath, Content = content });
        }

        var proposals = await _claude.AnalyzeFeaturesAsync(
            project.FullName, project.Language, codeFiles, readme, project.Description);

        int generated = 0;
        foreach (var p in proposals)
        {
            var exists = await _db.ProjectFeatures.AnyAsync(f => f.ProjectId == project.Id && f.Title == p.Title);
            if (exists) continue;

            _db.ProjectFeatures.Add(new ProjectFeature
            {
                ProjectId = project.Id,
                Title = p.Title,
                Description = p.Description,
                Implementation = p.Implementation,
                FilesToModify = p.FilesToModify,
                Complexity = p.Complexity
            });
            generated++;
        }

        Log("features", $"Features propuestas: {generated} nuevas", "features",
            $"IA analizo archivos clave y propuso {generated} features para {project.Name}",
            projectId: project.Id, projectName: project.Name);

        await _db.SaveChangesAsync();
        var total = await _db.ProjectFeatures.CountAsync(f => f.ProjectId == project.Id);

        return Ok(new { generated, totalFeatures = total });
    }

    [HttpGet("projects/{id}/analyses")]
    public async Task<IActionResult> GetProjectAnalyses(int id)
    {
        var analyses = await _db.ProjectAnalyses
            .Where(a => a.ProjectId == id)
            .OrderByDescending(a => a.AnalyzedAt)
            .ToListAsync();

        return Ok(analyses.Select(a => new
        {
            a.Id,
            a.AnalyzedAt,
            a.Summary,
            a.RecommendationsGenerated,
            a.TotalRecommendations,
            a.DetectedStack,
            a.DetectedTools,
            a.FilesAnalyzed
        }));
    }

    [HttpPost("projects/{id}/features/{featureId}/notes")]
    public async Task<IActionResult> UpdateFeatureNotes(int id, int featureId, [FromBody] UpdateNotesRequest request)
    {
        var feature = await _db.ProjectFeatures.FirstOrDefaultAsync(f => f.ProjectId == id && f.Id == featureId);
        if (feature == null) return NotFound();
        feature.UserNotes = request.UserNotes ?? string.Empty;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("projects/{id}/features/backlog")]
    public async Task<IActionResult> MoveFeaturesToBacklog(int id, [FromBody] FeatureBacklogRequest request)
    {
        var features = await _db.ProjectFeatures
            .Where(f => f.ProjectId == id && request.FeatureIds.Contains(f.Id) && !f.AddedToBacklog)
            .ToListAsync();

        foreach (var feature in features)
        {
            _db.ProjectWorkItems.Add(new ProjectWorkItem
            {
                ProjectId = id,
                Title = feature.Title,
                Notes = $"{feature.Description}\n\nImplementacion:\n{feature.Implementation}",
                Status = "backlog",
                Source = "feature"
            });
            feature.AddedToBacklog = true;
        }

        if (features.Count > 0)
        {
            var project = await _db.Projects.FindAsync(id);
            Log("backlog", $"{features.Count} feature(s) al backlog", "features",
                string.Join(", ", features.Select(f => f.Title)),
                projectId: id, projectName: project?.Name ?? "");
        }

        await _db.SaveChangesAsync();
        return Ok(new { moved = features.Count });
    }

    [HttpPost("projects/{id}/recommendations/{recId}/notes")]
    public async Task<IActionResult> UpdateRecommendationNotes(int id, int recId, [FromBody] UpdateNotesRequest request)
    {
        var recommendation = await _db.ProjectRecommendations
            .FirstOrDefaultAsync(r => r.ProjectId == id && r.Id == recId);

        if (recommendation == null)
            return NotFound();

        recommendation.UserNotes = request.UserNotes ?? string.Empty;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("projects/{id}/recommendations/selection")]
    public async Task<IActionResult> UpdateRecommendationSelection(int id, [FromBody] RecommendationSelectionRequest request)
    {
        var recommendation = await _db.ProjectRecommendations
            .FirstOrDefaultAsync(r => r.ProjectId == id && r.Id == request.RecommendationId);

        if (recommendation == null)
            return NotFound();

        recommendation.Selected = request.Selected;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("projects/{id}/backlog")]
    public async Task<IActionResult> MoveSelectedRecommendationsToBacklog(int id)
    {
        var project = await _db.Projects.FindAsync(id);
        if (project == null || !project.IsActive)
            return NotFound();

        var selected = await _db.ProjectRecommendations
            .Where(r => r.ProjectId == id && r.Selected && !r.AddedToBacklog)
            .ToListAsync();

        foreach (var recommendation in selected)
        {
            _db.ProjectWorkItems.Add(new ProjectWorkItem
            {
                ProjectId = id,
                Title = recommendation.Title,
                Notes = recommendation.Notes,
                Status = "backlog",
                Source = "recommendation"
            });

            recommendation.AddedToBacklog = true;
            recommendation.Selected = false;
        }

        if (selected.Count > 0)
        {
            Log("backlog", $"{selected.Count} recomendacion(es) al backlog", "backlog",
                string.Join(", ", selected.Select(s => s.Title)),
                projectId: id, projectName: project.Name);
        }

        await _db.SaveChangesAsync();
        return Ok(new { moved = selected.Count });
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetGitHubUser()
    {
        var login = await _github.GetAuthenticatedUserAsync();
        if (login == null)
            return BadRequest(new { message = "No se pudo autenticar con GitHub. Verifica el token." });

        return Ok(new { login });
    }

    private static List<string> SelectKeyFiles(List<string> allFiles, string? language)
    {
        var selected = new List<string>();

        // Entry points and configs (always important)
        var entryPoints = new[]
        {
            "Program.cs", "Startup.cs", "Program.fs",
            "index.js", "index.ts", "server.js", "server.ts", "app.js", "app.ts",
            "main.py", "app.py", "manage.py",
            "main.go", "main.rs", "lib.rs",
            "index.php"
        };

        var configFiles = new[]
        {
            "package.json", "tsconfig.json", "docker-compose.yml", "docker-compose.yaml",
            "Dockerfile", "Makefile", "Cargo.toml", "go.mod",
            "requirements.txt", "pyproject.toml", "composer.json",
            ".env.example"
        };

        // Add entry points found
        foreach (var ep in entryPoints)
        {
            var matches = allFiles.Where(f => f.EndsWith($"/{ep}") || f == ep).ToList();
            selected.AddRange(matches.Take(2));
        }

        // Add configs found
        foreach (var cf in configFiles)
        {
            if (allFiles.Contains(cf))
                selected.Add(cf);
        }

        // Add source files based on language
        var extensions = (language?.ToLower()) switch
        {
            "c#" => new[] { ".cs" },
            "javascript" => new[] { ".js", ".jsx" },
            "typescript" => new[] { ".ts", ".tsx" },
            "python" => new[] { ".py" },
            "go" => new[] { ".go" },
            "rust" => new[] { ".rs" },
            "java" => new[] { ".java" },
            "php" => new[] { ".php" },
            "ruby" => new[] { ".rb" },
            _ => Array.Empty<string>()
        };

        if (extensions.Length > 0)
        {
            // Prioritize: controllers, services, models, routes, handlers
            var priorityDirs = new[] { "controller", "service", "model", "route", "handler", "middleware", "api", "src" };
            var sourceFiles = allFiles
                .Where(f => extensions.Any(ext => f.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !f.Contains("/test", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("/spec", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("__tests__", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("/bin/", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("/obj/", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains("/node_modules/", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains(".Designer.", StringComparison.OrdinalIgnoreCase)
                         && !f.Contains(".g.", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(f => priorityDirs.Any(d => f.Contains(d, StringComparison.OrdinalIgnoreCase)) ? 1 : 0)
                .ThenBy(f => f.Count(c => c == '/'))
                .ToList();

            var remaining = 12 - selected.Count;
            selected.AddRange(sourceFiles.Take(remaining));
        }

        return selected.Distinct().Take(12).ToList();
    }

    private void Log(string action, string title, string source, string detail = "", int? projectId = null, string projectName = "", string status = "done")
    {
        _db.ActivityLogs.Add(new ActivityLog
        {
            ProjectId = projectId,
            ProjectName = projectName,
            Action = action,
            Title = title,
            Detail = detail,
            Source = source,
            Status = status
        });
    }

    private sealed record ProjectRecommendationSeed(string Title, string Notes, string Category, int Priority);
}

public class ImportRepoRequest
{
    public long RepoId { get; set; }
}

public class RecommendationSelectionRequest
{
    public int RecommendationId { get; set; }
    public bool Selected { get; set; }
}

public class UpdateNotesRequest
{
    public string? UserNotes { get; set; }
}

public class FeatureBacklogRequest
{
    public List<int> FeatureIds { get; set; } = new();
}
