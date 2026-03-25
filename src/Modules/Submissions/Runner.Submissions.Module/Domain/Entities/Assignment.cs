using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Domain.Entities;

public class Assignment
{
    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public long GitLabProjectId { get; private set; }
    public AssignmentType Type { get; private set; }
    public int? CoverageThreshold { get; private set; }
    
    /// <summary>URL публичного шаблонного репозитория для выполнения задания студентом.</summary>
    public string? TemplateRepoUrl { get; private set; }

    // Navigation
    public ICollection<Submission> Submissions { get; private set; } = new List<Submission>();

    private Assignment() { }

    public static Assignment Create(
        string title,
        long gitLabProjectId,
        AssignmentType type,
        int? coverageThreshold = null,
        string? templateRepoUrl = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new Assignment
        {
            Id = Guid.NewGuid(),
            Title = title,
            GitLabProjectId = gitLabProjectId,
            Type = type,
            CoverageThreshold = coverageThreshold,
            TemplateRepoUrl = templateRepoUrl
        };
    }
}

