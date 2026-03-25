using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Runner.Runner.Module.Application.Interfaces;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Runner.Module.Application.Workers;

internal sealed class OutboxWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<OutboxWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxWorker started. Polling every {Interval}s.", _interval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "OutboxWorker unhandled error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ISubmissionsDbContext>();
        var gitLabClient = scope.ServiceProvider.GetRequiredService<IGitLabClient>();

        var messages = await db.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
            .OrderBy(m => m.CreatedAt)
            .Take(10)
            .ToListAsync(ct);

        foreach (var msg in messages)
        {
            try
            {
                var payload = JsonSerializer.Deserialize<PipelineTriggerPayload>(msg.Payload)
                    ?? throw new InvalidOperationException("Invalid outbox payload.");

                // Получаем GitLabProjectId из задания
                var assignment = await db.Assignments
                    .FirstOrDefaultAsync(a => a.Id == payload.AssignmentId, ct)
                    ?? throw new InvalidOperationException($"Assignment {payload.AssignmentId} not found.");

                var pipelineId = await gitLabClient.TriggerPipelineAsync(
                    assignment.GitLabProjectId,
                    payload.StudentRepo,
                    payload.StudentBranch,
                    payload.SubmissionId,
                    ct);

                // Обновляем Submission
                var submission = await db.Submissions
                    .FirstOrDefaultAsync(s => s.Id == payload.SubmissionId, ct);

                if (submission is not null)
                    submission.SetTriggered(pipelineId);

                msg.MarkProcessed();
                await db.SaveChangesAsync(ct);

                logger.LogInformation(
                    "Pipeline {PipelineId} triggered for submission {SubmissionId}.",
                    pipelineId, payload.SubmissionId);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to process outbox message {MsgId}. RetryCount={Retry}.",
                    msg.Id, msg.RetryCount);

                msg.MarkFailed(ex.Message);
                await db.SaveChangesAsync(ct);
            }
        }
    }
}

