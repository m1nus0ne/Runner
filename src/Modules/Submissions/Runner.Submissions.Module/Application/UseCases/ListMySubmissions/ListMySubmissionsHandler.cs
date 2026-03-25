using Microsoft.EntityFrameworkCore;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Submissions.Module.Application.UseCases.ListMySubmissions;

internal sealed class ListMySubmissionsHandler(ISubmissionsDbContext db)
{
    public async Task<List<MySubmissionDto>> HandleAsync(
        ListMySubmissionsQuery query, CancellationToken ct = default)
    {
        var q = db.Submissions
            .Include(s => s.Assignment)
            .Include(s => s.CheckResult)
            .Where(s => s.StudentId == query.StudentId)
            .OrderByDescending(s => s.CreatedAt)
            .AsQueryable();

        if (query.AssignmentId.HasValue)
            q = q.Where(s => s.AssignmentId == query.AssignmentId.Value);

        if (query.Limit.HasValue)
            q = q.Take(query.Limit.Value);

        var submissions = await q.ToListAsync(ct);

        return submissions.Select(s => new MySubmissionDto(
            s.Id,
            s.AssignmentId,
            s.Assignment?.Title ?? "—",
            s.GitHubUrl,
            s.Branch,
            s.Status,
            s.CreatedAt,
            s.CheckResult?.PassedTests,
            s.CheckResult?.TotalTests)).ToList();
    }
}

