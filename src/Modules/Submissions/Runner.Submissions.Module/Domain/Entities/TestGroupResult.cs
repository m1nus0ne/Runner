using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Domain.Entities;

public class TestGroupResult
{
    public Guid Id { get; private set; }
    public Guid CheckResultId { get; private set; }
    public string GroupName { get; private set; } = string.Empty;
    public int Passed { get; private set; }
    public int Failed { get; private set; }
    public ErrorType? ErrorType { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Navigation
    public CheckResult? CheckResult { get; private set; }

    private TestGroupResult() { }

    public static TestGroupResult Create(
        Guid checkResultId,
        string groupName,
        int passed,
        int failed,
        ErrorType? errorType = null,
        string? errorMessage = null)
    {
        return new TestGroupResult
        {
            Id = Guid.NewGuid(),
            CheckResultId = checkResultId,
            GroupName = groupName,
            Passed = passed,
            Failed = failed,
            ErrorType = errorType,
            ErrorMessage = errorMessage
        };
    }
}

