using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Runner.Runner.Module.Application.UseCases.ProcessGitLabWebhook;
using Runner.Runner.Module.Infrastructure.GitLab;

namespace Runner.Runner.Module.Api;

internal static class WebhookEndpoints
{
    public static IEndpointRouteBuilder MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        // POST /webhooks/gitlab — принимает события пайплайна от GitLab
        app.MapPost("/webhooks/gitlab", async (
            HttpRequest request,
            GitLabWebhookPayload payload,
            ProcessGitLabWebhookHandler handler,
            IOptions<GitLabOptions> options,
            CancellationToken ct) =>
        {
            // Валидация секретного токена
            var token = request.Headers["X-Gitlab-Token"].FirstOrDefault();
            if (token != options.Value.WebhookSecret)
                return Results.Unauthorized();

            if (payload.ObjectKind != "pipeline")
                return Results.Ok();

            // Извлекаем SUBMISSION_ID из переменных пайплайна
            var variables = payload.ObjectAttributes.Variables ?? [];
            var submissionIdStr = variables.FirstOrDefault(v => v.Key == "SUBMISSION_ID")?.Value;

            if (!Guid.TryParse(submissionIdStr, out var submissionId))
                return Results.BadRequest("Missing or invalid SUBMISSION_ID variable.");

            // Находим job с артефактом (этап test)
            var testJob = payload.Builds?
                .FirstOrDefault(b => b.Stage == "test" && b.Status == "success");

            var cmd = new ProcessGitLabWebhookCommand(
                PipelineId:      payload.ObjectAttributes.Id,
                Status:          payload.ObjectAttributes.Status,
                GitLabProjectId: payload.Project?.Id ?? 0,
                JobId:           testJob?.Id ?? 0,
                SubmissionId:    submissionId);

            await handler.HandleAsync(cmd, ct);
            return Results.Ok();
        });

        return app;
    }
}

