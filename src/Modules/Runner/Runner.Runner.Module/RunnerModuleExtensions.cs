using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Runner.Runner.Module.Api;
using Runner.Runner.Module.Application.Interfaces;
using Runner.Runner.Module.Application.UseCases.ProcessGitLabWebhook;
using Runner.Runner.Module.Application.Workers;
using Runner.Runner.Module.Infrastructure.GitLab;

namespace Runner.Runner.Module;

public static class RunnerModuleExtensions
{
    public static IServiceCollection AddRunnerModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Options
        services.Configure<GitLabOptions>(
            configuration.GetSection(GitLabOptions.SectionName));
        services.Configure<OutboxOptions>(
            configuration.GetSection(OutboxOptions.SectionName));

        // GitLab HTTP client
        services.AddHttpClient<IGitLabClient, GitLabClient>((sp, client) =>
        {
            var opts = configuration
                .GetSection(GitLabOptions.SectionName)
                .Get<GitLabOptions>() ?? new();
            client.BaseAddress = new Uri(opts.BaseUrl);
            client.DefaultRequestHeaders.Add("PRIVATE-TOKEN", opts.ServiceToken);
        });

        // Use-case handlers
        services.AddScoped<ProcessGitLabWebhookHandler>();

        // Background worker
        services.AddHostedService<OutboxWorker>();

        return services;
    }

    public static IEndpointRouteBuilder MapRunnerModuleEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapWebhookEndpoints();
        return app;
    }
}

