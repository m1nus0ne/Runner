using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Application.UseCases.GetSubmission;

public record SubmissionDto(
    Guid Id,
    string StudentId,
    Guid AssignmentId,
    string GitHubUrl,
    string Branch,
    SubmissionStatus Status,
    DateTime CreatedAt,
    int? PassedTests,
    int? TotalTests);

