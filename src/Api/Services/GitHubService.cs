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

    public async Task<GitHubRepoDetail?> GetRepoDetailAsync(string fullName)
    {
        var response = await _http.GetAsync($"https://api.github.com/repos/{fullName}");
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<GitHubRepoDetail>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        });
    }

    public async Task<List<GitHubCommit>> GetRepoCommitsAsync(string fullName, int count = 20)
    {
        var response = await _http.GetAsync(
            $"https://api.github.com/repos/{fullName}/commits?per_page={count}");
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<GitHubCommit>>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        }) ?? new();
    }

    public async Task<string?> GetRepoReadmeAsync(string fullName)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{fullName}/readme");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<List<GitHubTreeItem>> GetRepoTreeAsync(string fullName, string branch = "main")
    {
        var response = await _http.GetAsync(
            $"https://api.github.com/repos/{fullName}/git/trees/{branch}?recursive=1");
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var tree = doc.RootElement.GetProperty("tree");
        var items = new List<GitHubTreeItem>();

        foreach (var item in tree.EnumerateArray())
        {
            items.Add(new GitHubTreeItem
            {
                Path = item.GetProperty("path").GetString() ?? "",
                Type = item.GetProperty("type").GetString() ?? "",
                Size = item.TryGetProperty("size", out var size) ? size.GetInt64() : 0
            });
        }

        return items;
    }

    public async Task<string?> GetFileContentAsync(string fullName, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"https://api.github.com/repos/{fullName}/contents/{path}");
        request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/vnd.github.raw+json"));

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadAsStringAsync();
    }

    public async Task<List<GitHubLanguageEntry>> GetRepoLanguagesAsync(string fullName)
    {
        var response = await _http.GetAsync($"https://api.github.com/repos/{fullName}/languages");
        if (!response.IsSuccessStatusCode) return new();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var result = new List<GitHubLanguageEntry>();

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            result.Add(new GitHubLanguageEntry { Name = prop.Name, Bytes = prop.Value.GetInt64() });
        }

        return result.OrderByDescending(l => l.Bytes).ToList();
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

public class GitHubRepoDetail
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
    public int OpenIssuesCount { get; set; }
    public string DefaultBranch { get; set; } = "main";
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime PushedAt { get; set; }
}

public class GitHubCommit
{
    public string Sha { get; set; } = string.Empty;
    public string HtmlUrl { get; set; } = string.Empty;
    public GitHubCommitDetail Commit { get; set; } = new();
    public GitHubCommitAuthorInfo? Author { get; set; }
}

public class GitHubCommitDetail
{
    public string Message { get; set; } = string.Empty;
    public GitHubCommitAuthor Author { get; set; } = new();
}

public class GitHubCommitAuthor
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}

public class GitHubCommitAuthorInfo
{
    public string Login { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class GitHubTreeItem
{
    public string Path { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long Size { get; set; }
}

public class GitHubLanguageEntry
{
    public string Name { get; set; } = string.Empty;
    public long Bytes { get; set; }
}
