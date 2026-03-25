using System.Net.Http.Json;
using Runner.Runner.Module.Application.Interfaces;

namespace Runner.Runner.Module.Infrastructure.GitLab;

internal sealed class GitLabClient(
    HttpClient httpClient) : IGitLabClient
{
    public async Task<long> TriggerPipelineAsync(
        long projectId,
        string studentRepo,
        string studentBranch,
        Guid submissionId,
        CancellationToken ct = default)
    {
        var request = new TriggerPipelineRequest(
            Ref: "main",
            Variables:
            [
                new("STUDENT_REPO", studentRepo),
                new("STUDENT_BRANCH", studentBranch),
                new("SUBMISSION_ID", submissionId.ToString())
            ]);

        var response = await httpClient.PostAsJsonAsync(
            $"/api/v4/projects/{projectId}/pipeline",
            request,
            ct);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TriggerPipelineResponse>(ct)
            ?? throw new InvalidOperationException("Empty response from GitLab pipeline trigger.");

        return result.Id;
    }

    public async Task<string> DownloadNUnitArtifactAsync(
        long projectId,
        long jobId,
        CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync(
            $"/api/v4/projects/{projectId}/jobs/{jobId}/artifacts/TestResult.xml",
            ct);

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStringAsync(ct);
    }
}

