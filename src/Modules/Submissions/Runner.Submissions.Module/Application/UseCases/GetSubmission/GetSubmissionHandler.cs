using Microsoft.EntityFrameworkCore;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Submissions.Module.Application.UseCases.GetSubmission;

internal sealed class GetSubmissionHandler(ISubmissionsDbContext db)
{
    public async Task<SubmissionDto> HandleAsync(GetSubmissionQuery query, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .Include(s => s.CheckResult)
            .FirstOrDefaultAsync(s => s.Id == query.SubmissionId, ct)
            ?? throw new NotFoundException($"Submission {query.SubmissionId} not found.");

        if (!query.IsAdmin && submission.StudentId != query.RequestingStudentId)
            throw new ForbiddenException("Access denied.");

        return new SubmissionDto(
            submission.Id,
            submission.StudentId,
            submission.AssignmentId,
            submission.GitHubUrl,
            submission.Branch,
            submission.Status,
            submission.CreatedAt,
            submission.CheckResult?.PassedTests,
            submission.CheckResult?.TotalTests);
    }
}

