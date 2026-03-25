using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Application.UseCases.CreateAssignment;

public record CreateAssignmentCommand(
    string Title,
    long GitLabProjectId,
    AssignmentType Type,
    int? CoverageThreshold = null,
    string? TemplateRepoUrl = null);

