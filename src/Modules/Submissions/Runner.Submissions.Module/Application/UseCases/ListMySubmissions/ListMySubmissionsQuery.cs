namespace Runner.Submissions.Module.Application.UseCases.ListMySubmissions;

public record ListMySubmissionsQuery(string StudentId, Guid? AssignmentId = null, int? Limit = null);

