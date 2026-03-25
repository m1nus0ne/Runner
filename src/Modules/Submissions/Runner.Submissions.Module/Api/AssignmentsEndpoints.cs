using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Runner.Submissions.Module.Application.UseCases.CreateAssignment;
using Runner.Submissions.Module.Application.UseCases.GetAssignment;
using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Api;

internal static class AssignmentsEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/assignments");

        // GET /assignments — список всех заданий (доступно авторизованным)
        group.MapGet("/", async (
            GetAssignmentHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.HandleListAsync(ct);
            return Results.Ok(list);
        }).RequireAuthorization();

        // GET /assignments/{id} — одно задание (доступно авторизованным)
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetAssignmentHandler handler,
            CancellationToken ct) =>
        {
            var dto = await handler.HandleAsync(id, ct);
            return Results.Ok(dto);
        }).RequireAuthorization();

        // POST /assignments — создание (только админ)
        group.MapPost("/", async (
            CreateAssignmentRequest req,
            CreateAssignmentHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(
                new CreateAssignmentCommand(req.Title, req.GitLabProjectId, req.Type, req.CoverageThreshold, req.TemplateRepoUrl), ct);

            var link = $"/assignments/{id}";
            return Results.Created(link, new { id, link });
        }).RequireAuthorization("AdminOnly");

        return app;
    }
}

internal record CreateAssignmentRequest(
    string Title,
    long GitLabProjectId,
    AssignmentType Type,
    int? CoverageThreshold,
    string? TemplateRepoUrl = null);

