using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;

public record SubmissionReportDto(
    Guid SubmissionId,
    SubmissionStatus Status,
    int TotalTests,
    int PassedTests,
    int FailedTests,
    IReadOnlyList<TestGroupResultDto> Groups);

public record TestGroupResultDto(
    string GroupName,
    int Passed,
    int Failed,
    ErrorType? ErrorType,
    string? ErrorMessage);

