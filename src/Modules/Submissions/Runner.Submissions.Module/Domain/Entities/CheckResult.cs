namespace Runner.Submissions.Module.Domain.Entities;

public class CheckResult
{
    public Guid Id { get; private set; }
    public Guid SubmissionId { get; private set; }
    public int TotalTests { get; private set; }
    public int PassedTests { get; private set; }
    public int FailedTests { get; private set; }
    public string RawNUnitXml { get; private set; } = string.Empty;

    // Navigation
    public Submission? Submission { get; private set; }
    public ICollection<TestGroupResult> TestGroupResults { get; private set; } = new List<TestGroupResult>();

    private CheckResult() { }

    public static CheckResult Create(
        Guid submissionId,
        int totalTests,
        int passedTests,
        int failedTests,
        string rawNUnitXml)
    {
        return new CheckResult
        {
            Id = Guid.NewGuid(),
            SubmissionId = submissionId,
            TotalTests = totalTests,
            PassedTests = passedTests,
            FailedTests = failedTests,
            RawNUnitXml = rawNUnitXml
        };
    }
}
