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
    IReadOnlyList<FailedTestDetailDto>? FailedTests);

public record FailedTestDetailDto(
    string TestName,
    string Message,
    string? Expected,
    string? Actual);

