namespace Runner.Runner.Module.Infrastructure.GitLab;

internal sealed class GitLabOptions
{
    public const string SectionName = "GitLab";

    public string BaseUrl { get; set; } = "https://gitlab.com";
    public string ServiceToken { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}

