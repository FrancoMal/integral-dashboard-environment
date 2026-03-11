using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Components;
using Web.Models;

namespace Web.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthService _authService;
    private readonly NavigationManager _navigation;

    public ApiClient(HttpClient http, AuthService authService, NavigationManager navigation)
    {
        _http = http;
        _authService = authService;
        _navigation = navigation;
    }

    public async Task<DashboardStats?> GetDashboardStatsAsync()
    {
        return await GetAsync<DashboardStats>("/api/dashboard/stats");
    }

    public async Task<VpsStats?> GetVpsStatsAsync()
    {
        return await GetAsync<VpsStats>("/api/system/stats");
    }

    public async Task<ActivitySummaryDto?> GetActivitiesAsync()
    {
        return await GetAsync<ActivitySummaryDto>("/api/dashboard/activities");
    }

    public async Task<UserDto?> GetMeAsync()
    {
        return await GetAsync<UserDto>("/api/auth/me");
    }

    public async Task<List<GitHubRepoDto>?> GetGitHubReposAsync()
    {
        return await GetAsync<List<GitHubRepoDto>>("/api/github/repos");
    }

    public async Task<List<ProjectDto>?> GetProjectsAsync()
    {
        return await GetAsync<List<ProjectDto>>("/api/github/projects");
    }

    public async Task<ProjectDto?> ImportRepoAsync(long repoId)
    {
        return await PostAsync<ProjectDto>("/api/github/import", new { repoId });
    }

    public async Task RemoveProjectAsync(int projectId)
    {
        await DeleteAsync($"/api/github/projects/{projectId}");
    }

    public async Task<ProjectDetailDto?> GetProjectDetailAsync(int projectId)
    {
        return await GetAsync<ProjectDetailDto>($"/api/github/projects/{projectId}/detail");
    }

    public async Task<AnalyzeProjectResultDto?> AnalyzeProjectAsync(int projectId)
    {
        return await PostAsync<AnalyzeProjectResultDto>($"/api/github/projects/{projectId}/analyze", new { });
    }

    public async Task UpdateRecommendationSelectionAsync(int projectId, int recommendationId, bool selected)
    {
        await PostAsync<object>($"/api/github/projects/{projectId}/recommendations/selection", new { recommendationId, selected });
    }

    public async Task<int> MoveSelectedRecommendationsToBacklogAsync(int projectId)
    {
        var result = await PostAsync<BacklogMoveResultDto>($"/api/github/projects/{projectId}/backlog", new { });
        return result?.Moved ?? 0;
    }

    private async Task<T?> GetAsync<T>(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task<T?> PostAsync<T>(string url, object body)
    {
        await SetAuthHeaderAsync();
        var response = await _http.PostAsJsonAsync(url, body);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return default;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task DeleteAsync(string url)
    {
        await SetAuthHeaderAsync();
        var response = await _http.DeleteAsync(url);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateTo("/login", forceLoad: true);
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    private sealed class BacklogMoveResultDto
    {
        [JsonPropertyName("moved")]
        public int Moved { get; set; }
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }
}
