namespace Runner.Runner.Module.Application.Interfaces;

/// <summary>Клиент для взаимодействия с GitLab API.</summary>
public interface IGitLabClient
{
    /// <summary>Запускает пайплайн и возвращает его ID.</summary>
    Task<long> TriggerPipelineAsync(
        long projectId,
        string studentRepo,
        string studentBranch,
        Guid submissionId,
        CancellationToken ct = default);

    /// <summary>Скачивает артефакт TestResult.xml (NUnit) для указанного job.</summary>
    Task<string> DownloadNUnitArtifactAsync(
        long projectId,
        long jobId,
        CancellationToken ct = default);

    /// <summary>Возвращает текущий статус пайплайна (created, pending, running, success, failed, canceled, …).</summary>
    Task<string> GetPipelineStatusAsync(long projectId, long pipelineId, CancellationToken ct = default);

    /// <summary>Возвращает список job-ов пайплайна.</summary>
    Task<IReadOnlyList<PipelineJobInfo>> GetPipelineJobsAsync(long projectId, long pipelineId, CancellationToken ct = default);
}

/// <summary>Краткая информация о job пайплайна.</summary>
public sealed record PipelineJobInfo(long Id, string Name, string Stage, string Status);

