using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Runner.Parsers.Module.Application.Interfaces;
using Runner.Runner.Module.Application.Interfaces;
using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;
using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Runner.Module.Application.Workers;

/// <summary>
/// Поллер пайплайнов: опрашивает GitLab API для submissions в статусе Triggered/Running,
/// при завершении скачивает артефакты и парсит результаты.
/// Временная замена webhook-механизму.
/// </summary>
internal sealed class PipelinePollerWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<OutboxOptions> options,
    ILogger<PipelinePollerWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(options.Value.PollingIntervalSeconds);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PipelinePollerWorker started. Polling every {Interval}s.", _interval.TotalSeconds);

        // Даём время OutboxWorker'у запустить пайплайны
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollTriggeredSubmissionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "PipelinePollerWorker unhandled error.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task PollTriggeredSubmissionsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ISubmissionsDbContext>();
        var gitLabClient = scope.ServiceProvider.GetRequiredService<IGitLabClient>();
        var parser = scope.ServiceProvider.GetRequiredService<INUnitXmlParser>();

        // Берём submissions, у которых пайплайн запущен, но результат ещё не получен
        var submissions = await db.Submissions
            .Include(s => s.Assignment)
            .Where(s => (s.Status == SubmissionStatus.Triggered || s.Status == SubmissionStatus.Running)
                        && s.GitLabPipelineId != null)
            .Take(10)
            .ToListAsync(ct);

        foreach (var submission in submissions)
        {
            try
            {
                await ProcessSubmissionAsync(submission, db, gitLabClient, parser, ct);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Failed to poll pipeline for submission {Id}, pipeline {PipelineId}.",
                    submission.Id, submission.GitLabPipelineId);
            }
        }
    }

    private async Task ProcessSubmissionAsync(
        Submission submission,
        ISubmissionsDbContext db,
        IGitLabClient gitLabClient,
        INUnitXmlParser parser,
        CancellationToken ct)
    {
        var projectId = submission.Assignment!.GitLabProjectId;
        var pipelineId = submission.GitLabPipelineId!.Value;

        // 1. Проверяем статус пайплайна
        var status = await gitLabClient.GetPipelineStatusAsync(projectId, pipelineId, ct);

        logger.LogInformation(
            "Pipeline {PipelineId} for submission {SubmissionId}: status={Status}",
            pipelineId, submission.Id, status);

        switch (status)
        {
            case "running" or "pending" or "created" or "waiting_for_resource" or "preparing":
                // Ещё выполняется — обновим статус и ждём
                if (submission.Status == SubmissionStatus.Triggered)
                    submission.SetRunning();
                await db.SaveChangesAsync(ct);
                return;

            case "canceled":
                submission.SetTimeout();
                await db.SaveChangesAsync(ct);
                return;

            case "failed":
                // Пайплайн упал — пробуем всё же скачать артефакты (тесты могли пройти частично)
                await TryProcessArtifactsAsync(submission, projectId, pipelineId, db, gitLabClient, parser, ct);
                return;

            case "success":
                await TryProcessArtifactsAsync(submission, projectId, pipelineId, db, gitLabClient, parser, ct);
                return;

            default:
                logger.LogWarning("Unknown pipeline status '{Status}' for submission {Id}", status, submission.Id);
                return;
        }
    }

    private async Task TryProcessArtifactsAsync(
        Submission submission,
        long projectId,
        long pipelineId,
        ISubmissionsDbContext db,
        IGitLabClient gitLabClient,
        INUnitXmlParser parser,
        CancellationToken ct)
    {
        try
        {
            // 2. Получаем список job-ов пайплайна
            var jobs = await gitLabClient.GetPipelineJobsAsync(projectId, pipelineId, ct);

            // Ищем test job (успешный или упавший — артефакты доступны при artifacts:when:always)
            var testJob = jobs.FirstOrDefault(j => j.Stage == "test" && j.Status == "success")
                       ?? jobs.FirstOrDefault(j => j.Stage == "test" && j.Status == "failed")
                       ?? jobs.FirstOrDefault(j => j.Stage == "test");

            if (testJob is null)
            {
                logger.LogWarning("No test job found for pipeline {PipelineId}", pipelineId);
                submission.SetError();
                await db.SaveChangesAsync(ct);
                return;
            }

            // 3. Скачиваем TestResult.xml
            var rawXml = await gitLabClient.DownloadNUnitArtifactAsync(projectId, testJob.Id, ct);

            logger.LogInformation(
                "Downloaded TestResult.xml ({Length} chars) for submission {Id} from job {JobId}",
                rawXml.Length, submission.Id, testJob.Id);

            // 4. Парсим результат
            var tempId = Guid.NewGuid();
            var groups = parser.Parse(tempId, rawXml);

            int total  = groups.Sum(g => g.Passed + g.Failed);
            int passed = groups.Sum(g => g.Passed);
            int failed = groups.Sum(g => g.Failed);

            // 5. Сохраняем CheckResult
            var checkResult = CheckResult.Create(submission.Id, total, passed, failed, rawXml);
            db.CheckResults.Add(checkResult);

            // 6. Сохраняем TestGroupResults с реальным CheckResult.Id
            var finalGroups = parser.Parse(checkResult.Id, rawXml);
            foreach (var g in finalGroups)
                db.TestGroupResults.Add(g);

            // 7. Обновляем статус
            if (failed == 0 && total > 0)
                submission.SetPassed();
            else
                submission.SetFailed();

            await db.SaveChangesAsync(ct);

            logger.LogInformation(
                "Submission {Id}: {Passed}/{Total} tests passed → {Status}",
                submission.Id, passed, total, submission.Status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process artifacts for submission {Id}, pipeline {PipelineId}",
                submission.Id, pipelineId);
            submission.SetError();
            await db.SaveChangesAsync(ct);
        }
    }
}

