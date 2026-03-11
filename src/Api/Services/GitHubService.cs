using System.Net.Http.Headers;
using System.Text.Json;

namespace Api.Services;

public class GitHubService
{
    private readonly HttpClient _http;
    private readonly string? _token;

    public GitHubService(IConfiguration configuration)
    {
        _http = new HttpClient();
        _token = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                 ?? configuration["GitHub:Token"];

        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DashboardApp", "1.0"));
        if (!string.IsNullOrEmpty(_token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        }
    }

    public async Task<List<GitHubRepo>> GetUserReposAsync()
    {
        var allRepos = new List<GitHubRepo>();
        int page = 1;
        const int perPage = 100;

        while (true)
        {
            var response = await _http.GetAsync(
                $"https://api.github.com/user/repos?per_page={perPage}&page={page}&sort=updated&affiliation=owner");

            if (!response.IsSuccessStatusCode)
                break;

            var json = await response.Content.ReadAsStringAsync();
            var repos = JsonSerializer.Deserialize<List<GitHubRepo>>(json, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            if (repos == null || repos.Count == 0)
                break;

            allRepos.AddRange(repos);

            if (repos.Count < perPage)
                break;

            page++;
        }

        return allRepos;
    }

    public async Task<GitHubRepo?> GetRepoAsync(string fullName)
    {
        var response = await _http.GetAsync($"https://api.github.com/repos/{fullName}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRepo>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    public async Task<string?> GetAuthenticatedUserAsync()
    {
        var response = await _http.GetAsync("https://api.github.com/user");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("login").GetString();
    }
}

public class GitHubRepo
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string HtmlUrl { get; set; } = string.Empty;
    public string? Language { get; set; }
    public bool Private { get; set; }
    public int StargazersCount { get; set; }
    public int ForksCount { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
