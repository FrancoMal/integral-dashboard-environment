using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectRecommendation> ProjectRecommendations => Set<ProjectRecommendation>();
    public DbSet<ProjectWorkItem> ProjectWorkItems => Set<ProjectWorkItem>();
    public DbSet<ProjectAnalysis> ProjectAnalyses => Set<ProjectAnalysis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Username).IsUnique();
            entity.HasIndex(u => u.Email).IsUnique();
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasIndex(p => p.GitHubRepoId).IsUnique();
        });

        modelBuilder.Entity<ProjectRecommendation>(entity =>
        {
            entity.HasIndex(r => new { r.ProjectId, r.Title }).IsUnique();
        });

        modelBuilder.Entity<ProjectWorkItem>(entity =>
        {
            entity.HasIndex(w => w.ProjectId);
        });

        modelBuilder.Entity<ProjectAnalysis>(entity =>
        {
            entity.HasIndex(a => a.ProjectId);
        });
    }
}
