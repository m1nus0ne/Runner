using System.Net.Http.Json;
using System.Text.Json;
using Runner.Runner.Module.Application.Interfaces;

namespace Runner.Runner.Module.Infrastructure.GitLab;

internal sealed class GitLabClient(
    HttpClient httpClient) : IGitLabClient
{
    // ...existing TriggerPipelineAsync...
    public async Task<long> TriggerPipelineAsync(
        long projectId,
        string studentRepo,
        string studentBranch,
        Guid submissionId,
        CancellationToken ct = default)
    {
        var defaultBranch = await GetDefaultBranchAsync(projectId, ct);

        var request = new TriggerPipelineRequest(
            Ref: defaultBranch,
            Variables:
            [
                new("STUDENT_REPO", studentRepo),
                new("STUDENT_BRANCH", studentBranch),
                new("SUBMISSION_ID", submissionId.ToString())
            ]);

        var url = $"api/v4/projects/{projectId}/pipeline";
        var response = await httpClient.PostAsJsonAsync(url, request, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"GitLab API {response.StatusCode} at {httpClient.BaseAddress}{url}: {body}");
        }

        var result = await response.Content.ReadFromJsonAsync<TriggerPipelineResponse>(ct)
            ?? throw new InvalidOperationException("Empty response from GitLab pipeline trigger.");

        return result.Id;
    }

    // ...existing DownloadNUnitArtifactAsync...
    public async Task<string> DownloadNUnitArtifactAsync(
        long projectId,
        long jobId,
        CancellationToken ct = default)
    {
        var url = $"api/v4/projects/{projectId}/jobs/{jobId}/artifacts/TestResult.xml";
        var response = await httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"GitLab API {response.StatusCode} at {httpClient.BaseAddress}{url}: {body}");
        }

        return await response.Content.ReadAsStringAsync(ct);
    }

    public async Task<string> GetPipelineStatusAsync(
        long projectId, long pipelineId, CancellationToken ct = default)
    {
        var url = $"api/v4/projects/{projectId}/pipelines/{pipelineId}";
        var response = await httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"GitLab API {response.StatusCode} at {httpClient.BaseAddress}{url}: {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        return json.GetProperty("status").GetString() ?? "unknown";
    }

    public async Task<IReadOnlyList<PipelineJobInfo>> GetPipelineJobsAsync(
        long projectId, long pipelineId, CancellationToken ct = default)
    {
        var url = $"api/v4/projects/{projectId}/pipelines/{pipelineId}/jobs";
        var response = await httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException(
                $"GitLab API {response.StatusCode} at {httpClient.BaseAddress}{url}: {body}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        var jobs = new List<PipelineJobInfo>();

        foreach (var job in json.EnumerateArray())
        {
            jobs.Add(new PipelineJobInfo(
                Id: job.GetProperty("id").GetInt64(),
                Name: job.GetProperty("name").GetString() ?? "",
                Stage: job.GetProperty("stage").GetString() ?? "",
                Status: job.GetProperty("status").GetString() ?? ""));
        }

        return jobs;
    }

    private async Task<string> GetDefaultBranchAsync(long projectId, CancellationToken ct)
    {
        var url = $"api/v4/projects/{projectId}";
        var response = await httpClient.GetAsync(url, ct);

        if (!response.IsSuccessStatusCode)
            return "main";

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(ct);
        if (json.TryGetProperty("default_branch", out var branch) &&
            branch.GetString() is { Length: > 0 } branchName)
            return branchName;

        return "main";
    }
}

