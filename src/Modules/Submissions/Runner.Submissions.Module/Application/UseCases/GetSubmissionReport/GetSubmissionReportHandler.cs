using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;

internal sealed class GetSubmissionReportHandler(ISubmissionsDbContext db)
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true
    };

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
            .Select(g => new TestGroupResultDto(
                g.GroupName,
                g.Passed,
                g.Failed,
                g.ErrorType,
                ParseFailedTests(g.ErrorMessage)))
            .ToList();

        return new SubmissionReportDto(
            submission.Id,
            submission.Status,
            submission.CheckResult.TotalTests,
            submission.CheckResult.PassedTests,
            submission.CheckResult.FailedTests,
            groups);
    }

    /// <summary>
    /// Пробует десериализовать ErrorMessage как JSON-массив упавших тестов.
    /// Если не JSON — возвращает один элемент с message = rawString.
    /// </summary>
    private static IReadOnlyList<FailedTestDetailDto>? ParseFailedTests(string? errorMessage)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
            return null;

        try
        {
            var list = JsonSerializer.Deserialize<List<FailedTestDetailDto>>(errorMessage, JsonOpts);
            return list is { Count: > 0 } ? list : null;
        }
        catch
        {
            // Legacy plain-text error → wrap as single entry
            return [new FailedTestDetailDto("—", errorMessage, null, null)];
        }
    }
}

