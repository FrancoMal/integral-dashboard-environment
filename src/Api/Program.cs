using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Data;
using Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Authentication
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddSingleton<GitHubService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Template API",
        Version = "v1"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed: ensure admin user has a valid BCrypt hash
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var admin = await db.Users.FirstOrDefaultAsync(u => u.Username == "admin");
    if (admin is not null)
    {
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        await db.SaveChangesAsync();
    }
}

// Ensure database tables exist (Projects + analysis/backlog)
using (var migrateScope = app.Services.CreateScope())
{
    var migrateDb = migrateScope.ServiceProvider.GetRequiredService<AppDbContext>();
    await migrateDb.Database.EnsureCreatedAsync();

    // Migrate Projects table to match current model
    await migrateDb.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'GitHubRepoId')
    ALTER TABLE [Projects] ADD [GitHubRepoId] BIGINT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'FullName')
    ALTER TABLE [Projects] ADD [FullName] NVARCHAR(255) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'HtmlUrl')
    ALTER TABLE [Projects] ADD [HtmlUrl] NVARCHAR(500) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'Language')
    ALTER TABLE [Projects] ADD [Language] NVARCHAR(100) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'IsPrivate')
    ALTER TABLE [Projects] ADD [IsPrivate] BIT NOT NULL DEFAULT 0;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'ImportedAt')
    ALTER TABLE [Projects] ADD [ImportedAt] DATETIME2 NOT NULL DEFAULT GETDATE();

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[Projects]') AND name = 'IsActive')
    ALTER TABLE [Projects] ADD [IsActive] BIT NOT NULL DEFAULT 1;
");

    await migrateDb.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[ProjectRecommendations]', N'U') IS NULL
BEGIN
    CREATE TABLE [ProjectRecommendations] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProjectId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Notes] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(50) NOT NULL,
        [Priority] INT NOT NULL,
        [Selected] BIT NOT NULL DEFAULT 0,
        [AddedToBacklog] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL
    );
    CREATE UNIQUE INDEX [IX_ProjectRecommendations_ProjectId_Title] ON [ProjectRecommendations]([ProjectId], [Title]);
END
");

    await migrateDb.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[ProjectWorkItems]', N'U') IS NULL
BEGIN
    CREATE TABLE [ProjectWorkItems] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProjectId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Notes] NVARCHAR(MAX) NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,
        [Source] NVARCHAR(50) NOT NULL,
        [CreatedAt] DATETIME2 NOT NULL
    );
    CREATE INDEX [IX_ProjectWorkItems_ProjectId] ON [ProjectWorkItems]([ProjectId]);
END
");

    await migrateDb.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'[ProjectAnalyses]', N'U') IS NULL
BEGIN
    CREATE TABLE [ProjectAnalyses] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProjectId] INT NOT NULL,
        [AnalyzedAt] DATETIME2 NOT NULL,
        [Summary] NVARCHAR(MAX) NOT NULL,
        [RecommendationsGenerated] INT NOT NULL,
        [TotalRecommendations] INT NOT NULL,
        [DetectedStack] NVARCHAR(500) NOT NULL DEFAULT '',
        [DetectedTools] NVARCHAR(500) NOT NULL DEFAULT '',
        [FilesAnalyzed] INT NOT NULL DEFAULT 0
    );
    CREATE INDEX [IX_ProjectAnalyses_ProjectId] ON [ProjectAnalyses]([ProjectId]);
END
");

    await migrateDb.Database.ExecuteSqlRawAsync(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRecommendations]') AND name = 'AnalysisId')
    ALTER TABLE [ProjectRecommendations] ADD [AnalysisId] INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRecommendations]') AND name = 'Notes')
    ALTER TABLE [ProjectRecommendations] ADD [Notes] NVARCHAR(MAX) NOT NULL DEFAULT '';

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ProjectRecommendations]') AND name = 'AddedToBacklog')
    ALTER TABLE [ProjectRecommendations] ADD [AddedToBacklog] BIT NOT NULL DEFAULT 0;
");
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
