using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;
using Runner.SharedKernel;

namespace Runner.Submissions.Module.Application.UseCases.CreateSubmission;

internal sealed class CreateSubmissionHandler(ISubmissionsDbContext db)
{
    public async Task<Guid> HandleAsync(CreateSubmissionCommand cmd, CancellationToken ct = default)
    {
        var assignmentExists = await db.Assignments
            .AnyAsync(a => a.Id == cmd.AssignmentId, ct);

        if (!assignmentExists)
            throw new NotFoundException($"Assignment {cmd.AssignmentId} not found.");

        var submission = Submission.Create(
            cmd.StudentId,
            cmd.AssignmentId,
            cmd.GitHubUrl,
            cmd.Branch);

        var payload = JsonSerializer.Serialize(new PipelineTriggerPayload(
            submission.Id,
            cmd.AssignmentId,
            cmd.GitHubUrl,
            cmd.Branch));

        var outbox = OutboxMessage.Create(payload);

        db.Submissions.Add(submission);
        db.OutboxMessages.Add(outbox);
        await db.SaveChangesAsync(ct);

        return submission.Id;
    }
}

