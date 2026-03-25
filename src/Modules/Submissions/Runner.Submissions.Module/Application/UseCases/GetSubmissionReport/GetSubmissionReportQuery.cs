namespace Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;

public record GetSubmissionReportQuery(Guid SubmissionId, string RequestingStudentId, bool IsAdmin);

