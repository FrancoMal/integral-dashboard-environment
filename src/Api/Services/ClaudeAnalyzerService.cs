using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Services;

public class ClaudeAnalyzerService
{
    private readonly HttpClient _http;
    private readonly string _baseUrl;

    public ClaudeAnalyzerService(IConfiguration configuration)
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        _baseUrl = Environment.GetEnvironmentVariable("CLAUDE_ANALYZER_URL")
                   ?? configuration["ClaudeAnalyzer:Url"]
                   ?? "http://claude-analyzer:4500";
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var response = await _http.GetAsync($"{_baseUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<ClaudeRecommendation>> AnalyzeCodeAsync(
        string repoName, string? language, List<CodeFile> files)
    {
        try
        {
            var payload = new
            {
                repoName,
                language = language ?? "unknown",
                files = files.Select(f => new { f.Path, f.Content }).ToList()
            };

            var response = await _http.PostAsJsonAsync($"{_baseUrl}/analyze", payload);

            if (!response.IsSuccessStatusCode)
                return new();

            var result = await response.Content.ReadFromJsonAsync<AnalyzeResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Recommendations ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClaudeAnalyzer] Error: {ex.Message}");
            return new();
        }
    }
    public async Task<List<FeatureProposal>> AnalyzeFeaturesAsync(
        string repoName, string? language, List<CodeFile> files, string? readme, string? description)
    {
        try
        {
            var payload = new
            {
                repoName,
                language = language ?? "unknown",
                files = files.Select(f => new { f.Path, f.Content }).ToList(),
                readme = readme ?? "",
                description = description ?? ""
            };

            var response = await _http.PostAsJsonAsync($"{_baseUrl}/features", payload);

            if (!response.IsSuccessStatusCode)
                return new();

            var result = await response.Content.ReadFromJsonAsync<FeaturesResponse>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return result?.Features ?? new();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ClaudeAnalyzer] Features error: {ex.Message}");
            return new();
        }
    }
}

public class CodeFile
{
    public string Path { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class ClaudeRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string Category { get; set; } = "general";
    public int Priority { get; set; } = 2;
}

public class AnalyzeResponse
{
    public List<ClaudeRecommendation> Recommendations { get; set; } = new();
    public string? Raw { get; set; }
}

public class FeatureProposal
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Implementation { get; set; } = string.Empty;
    public string FilesToModify { get; set; } = string.Empty;
    public string Complexity { get; set; } = "media";
}

public class FeaturesResponse
{
    public List<FeatureProposal> Features { get; set; } = new();
    public string? Raw { get; set; }
}
