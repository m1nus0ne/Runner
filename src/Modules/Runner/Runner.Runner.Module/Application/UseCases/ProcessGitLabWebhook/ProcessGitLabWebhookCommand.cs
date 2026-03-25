namespace Runner.Runner.Module.Application.UseCases.ProcessGitLabWebhook;

/// <summary>
/// Данные из вебхука GitLab при завершении пайплайна.
/// status: "success" | "failed" | "canceled"
/// </summary>
public record ProcessGitLabWebhookCommand(
    long PipelineId,
    string Status,
    long GitLabProjectId,
    long JobId,
    Guid SubmissionId);

