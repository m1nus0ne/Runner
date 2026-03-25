using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Runner.Submissions.Module.Application.UseCases.CreateAssignment;
using Runner.Submissions.Module.Domain.Enums;

namespace Runner.Submissions.Module.Api;

internal static class AssignmentsEndpoints
{
    public static IEndpointRouteBuilder MapAssignmentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/assignments").RequireAuthorization("AdminOnly");

        group.MapPost("/", async (
            CreateAssignmentRequest req,
            CreateAssignmentHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.HandleAsync(
                new CreateAssignmentCommand(req.Title, req.GitLabProjectId, req.Type, req.CoverageThreshold, req.TemplateRepoUrl), ct);
            return Results.Created($"/assignments/{id}", new { id });
        });

        return app;
    }
}

internal record CreateAssignmentRequest(
    string Title,
    long GitLabProjectId,
    AssignmentType Type,
    int? CoverageThreshold,
    string? TemplateRepoUrl = null);

