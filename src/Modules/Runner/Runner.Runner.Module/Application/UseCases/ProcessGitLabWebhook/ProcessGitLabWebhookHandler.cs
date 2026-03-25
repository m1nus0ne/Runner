using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Runner.Parsers.Module.Application.Interfaces;
using Runner.Runner.Module.Application.Interfaces;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Runner.Module.Application.UseCases.ProcessGitLabWebhook;

internal sealed class ProcessGitLabWebhookHandler(
    ISubmissionsDbContext db,
    IGitLabClient gitLabClient,
    INUnitXmlParser nUnitParser,
    ILogger<ProcessGitLabWebhookHandler> logger)
{
    public async Task HandleAsync(ProcessGitLabWebhookCommand cmd, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .FirstOrDefaultAsync(s => s.Id == cmd.SubmissionId, ct)
            ?? throw new NotFoundException($"Submission {cmd.SubmissionId} not found.");

        switch (cmd.Status)
        {
            case "canceled":
                submission.SetTimeout();
                await db.SaveChangesAsync(ct);
                return;

            case "failed":
                submission.SetError();
                await db.SaveChangesAsync(ct);
                return;

            case "success":
                await HandleSuccessAsync(submission, cmd, ct);
                return;

            default:
                logger.LogWarning("Unknown pipeline status '{Status}' for submission {Id}", cmd.Status, cmd.SubmissionId);
                return;
        }
    }

    private async Task HandleSuccessAsync(
        Submission submission,
        ProcessGitLabWebhookCommand cmd,
        CancellationToken ct)
    {
        try
        {
            var rawXml = await gitLabClient.DownloadNUnitArtifactAsync(
                cmd.GitLabProjectId, cmd.JobId, ct);

            // Сначала парсим, чтобы знать итоговые цифры
            var tempId = Guid.NewGuid();
            var groups = nUnitParser.Parse(tempId, rawXml);

            int total  = groups.Sum(g => g.Passed + g.Failed);
            int passed = groups.Sum(g => g.Passed);
            int failed = groups.Sum(g => g.Failed);

            // Создаём CheckResult с правильными итогами
            var checkResult = CheckResult.Create(submission.Id, total, passed, failed, rawXml);
            db.CheckResults.Add(checkResult);

            // Перепарсиваем с реальным ID
            var finalGroups = nUnitParser.Parse(checkResult.Id, rawXml);
            foreach (var g in finalGroups)
                db.TestGroupResults.Add(g);

            if (failed == 0)
                submission.SetPassed();
            else
                submission.SetFailed();

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process NUnit artifact for submission {Id}", submission.Id);
            submission.SetError();
            await db.SaveChangesAsync(ct);
        }
    }
}

