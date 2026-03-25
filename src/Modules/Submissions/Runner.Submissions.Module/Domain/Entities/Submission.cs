using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Domain.Entities;

public class Submission
{
    public Guid Id { get; private set; }
    public string StudentId { get; private set; } = string.Empty;
    public Guid AssignmentId { get; private set; }
    public string GitHubUrl { get; private set; } = string.Empty;
    public string Branch { get; private set; } = string.Empty;
    public long? GitLabPipelineId { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public Assignment? Assignment { get; private set; }
    public CheckResult? CheckResult { get; private set; }

    private Submission() { }

    public static Submission Create(
        string studentId,
        Guid assignmentId,
        string gitHubUrl,
        string branch)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(studentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(gitHubUrl);
        ArgumentException.ThrowIfNullOrWhiteSpace(branch);

        return new Submission
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            AssignmentId = assignmentId,
            GitHubUrl = gitHubUrl,
            Branch = branch,
            Status = SubmissionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetTriggered(long pipelineId)
    {
        GitLabPipelineId = pipelineId;
        Status = SubmissionStatus.Triggered;
    }

    public void SetRunning() => Status = SubmissionStatus.Running;
    public void SetPassed() => Status = SubmissionStatus.Passed;
    public void SetFailed() => Status = SubmissionStatus.Failed;
    public void SetError() => Status = SubmissionStatus.Error;
    public void SetTimeout() => Status = SubmissionStatus.Timeout;
}

