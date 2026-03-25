using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Application.UseCases.CreateAssignment;

internal sealed class CreateAssignmentHandler(ISubmissionsDbContext db)
{
    public async Task<Guid> HandleAsync(CreateAssignmentCommand cmd, CancellationToken ct = default)
    {
        var assignment = Assignment.Create(
            cmd.Title,
            cmd.GitLabProjectId,
            cmd.Type,
            cmd.CoverageThreshold,
            cmd.TemplateRepoUrl);

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync(ct);

        return assignment.Id;
    }
}

