namespace Runner.Submissions.Module.Application.UseCases.CreateSubmission;

public record CreateSubmissionCommand(
    string StudentId,
    Guid AssignmentId,
    string GitHubUrl,
    string Branch);

