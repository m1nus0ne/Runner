using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Application.UseCases.ListMySubmissions;

public record MySubmissionDto(
    Guid Id,
    Guid AssignmentId,
    string AssignmentTitle,
    string GitHubUrl,
    string Branch,
    SubmissionStatus Status,
    DateTime CreatedAt,
    int? PassedTests,
    int? TotalTests);

