using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Application.UseCases.GetAssignment;

public record AssignmentDto(
    Guid Id,
    string Title,
    AssignmentType Type,
    int? CoverageThreshold,
    string? TemplateRepoUrl);

