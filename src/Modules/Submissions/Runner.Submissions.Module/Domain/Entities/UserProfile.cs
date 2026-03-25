using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Domain.Entities;

public class UserProfile
{
    public Guid Id { get; private set; }
    public string GitHubLogin { get; private set; } = string.Empty;
    public long GitHubId { get; private set; }
    public UserRole Role { get; private set; }

    private UserProfile() { }

    public static UserProfile Create(string gitHubLogin, long gitHubId, UserRole role = UserRole.Student)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gitHubLogin);

        return new UserProfile
        {
            Id = Guid.NewGuid(),
            GitHubLogin = gitHubLogin,
            GitHubId = gitHubId,
            Role = role
        };
    }

    public void PromoteToAdmin() => Role = UserRole.Admin;
    public void DemoteToStudent() => Role = UserRole.Student;
}

