using Microsoft.EntityFrameworkCore;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;

internal sealed class GetSubmissionReportHandler(ISubmissionsDbContext db)
{
    public async Task<SubmissionReportDto> HandleAsync(GetSubmissionReportQuery query, CancellationToken ct = default)
    {
        var submission = await db.Submissions
            .Include(s => s.CheckResult)
                .ThenInclude(cr => cr!.TestGroupResults)
            .FirstOrDefaultAsync(s => s.Id == query.SubmissionId, ct)
            ?? throw new NotFoundException($"Submission {query.SubmissionId} not found.");

        if (!query.IsAdmin && submission.StudentId != query.RequestingStudentId)
            throw new ForbiddenException("Access denied.");

        if (submission.CheckResult is null)
            throw new NotFoundException($"Report for submission {query.SubmissionId} is not ready yet.");

        var groups = submission.CheckResult.TestGroupResults
            .Select(g => new TestGroupResultDto(g.GroupName, g.Passed, g.Failed, g.ErrorType, g.ErrorMessage))
            .ToList();

        return new SubmissionReportDto(
            submission.Id,
            submission.Status,
            submission.CheckResult.TotalTests,
            submission.CheckResult.PassedTests,
            submission.CheckResult.FailedTests,
            groups);
    }
}

