namespace Runner.SharedKernel;

/// <summary>Сообщение Outbox: данные для запуска пайплайна GitLab.</summary>
public sealed record PipelineTriggerPayload(
    Guid SubmissionId,
    Guid AssignmentId,
    string StudentRepo,
    string StudentBranch);

