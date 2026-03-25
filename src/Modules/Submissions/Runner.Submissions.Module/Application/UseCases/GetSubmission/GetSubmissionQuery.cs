namespace Runner.Submissions.Module.Application.UseCases.GetSubmission;

public record GetSubmissionQuery(Guid SubmissionId, string RequestingStudentId, bool IsAdmin);

