using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Runner.Submissions.Module.Application.UseCases.CreateSubmission;
using Runner.Submissions.Module.Application.UseCases.GetSubmission;
using Runner.Submissions.Module.Application.UseCases.GetSubmissionReport;
using Runner.Submissions.Module.Application.UseCases.ListMySubmissions;

namespace Runner.Submissions.Module.Api;

internal static class SubmissionsEndpoints
{
    public static IEndpointRouteBuilder MapSubmissionsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/submissions").RequireAuthorization();

        // GET /submissions/my — все попытки ученика (опциональная фильтрация по assignmentId)
        group.MapGet("/my", async (
            Guid? assignmentId,
            ClaimsPrincipal user,
            ListMySubmissionsHandler handler,
            CancellationToken ct) =>
        {
            var studentId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException();

            var list = await handler.HandleAsync(
                new ListMySubmissionsQuery(studentId, assignmentId), ct);
            return Results.Ok(list);
        });

        // GET /submissions/my/recent?limit=5 — последние N попыток для сайдбара
        group.MapGet("/my/recent", async (
            int? limit,
            ClaimsPrincipal user,
            ListMySubmissionsHandler handler,
            CancellationToken ct) =>
        {
            var studentId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException();

            var list = await handler.HandleAsync(
                new ListMySubmissionsQuery(studentId, Limit: limit ?? 5), ct);
            return Results.Ok(list);
        });

        // POST /submissions — студент создаёт отправку
        group.MapPost("/", async (
            CreateSubmissionRequest req,
            ClaimsPrincipal user,
            CreateSubmissionHandler handler,
            CancellationToken ct) =>
        {
            var studentId = user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAccessException();

            var id = await handler.HandleAsync(
                new CreateSubmissionCommand(studentId, req.AssignmentId, req.GitHubUrl, req.Branch), ct);

            return Results.Accepted($"/submissions/{id}", new { id });
        }).RequireAuthorization("StudentOnly");

        // GET /submissions/{id} — статус + passed/total
        group.MapGet("/{id:guid}", async (
            Guid id,
            ClaimsPrincipal user,
            GetSubmissionHandler handler,
            CancellationToken ct) =>
        {
            var studentId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin   = user.IsInRole("Admin");

            var dto = await handler.HandleAsync(new GetSubmissionQuery(id, studentId, isAdmin), ct);
            return Results.Ok(dto);
        });

        // GET /submissions/{id}/report — полный отчёт по группам
        group.MapGet("/{id:guid}/report", async (
            Guid id,
            ClaimsPrincipal user,
            GetSubmissionReportHandler handler,
            CancellationToken ct) =>
        {
            var studentId = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin   = user.IsInRole("Admin");

            var dto = await handler.HandleAsync(new GetSubmissionReportQuery(id, studentId, isAdmin), ct);
            return Results.Ok(dto);
        });

        return app;
    }
}

internal record CreateSubmissionRequest(Guid AssignmentId, string GitHubUrl, string Branch);

