using Microsoft.EntityFrameworkCore;
using Runner.SharedKernel;
using Runner.Submissions.Module.Application.Interfaces;

namespace Runner.Submissions.Module.Application.UseCases.GetAssignment;

internal sealed class GetAssignmentHandler(ISubmissionsDbContext db)
{
    public async Task<AssignmentDto> HandleAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var a = await db.Assignments
            .FirstOrDefaultAsync(x => x.Id == assignmentId, ct)
            ?? throw new NotFoundException($"Assignment {assignmentId} not found.");

        return new AssignmentDto(a.Id, a.Title, a.Type, a.CoverageThreshold, a.TemplateRepoUrl);
    }

    public async Task<IReadOnlyList<AssignmentDto>> HandleListAsync(CancellationToken ct = default)
    {
        return await db.Assignments
            .Select(a => new AssignmentDto(a.Id, a.Title, a.Type, a.CoverageThreshold, a.TemplateRepoUrl))
            .ToListAsync(ct);
    }
}

