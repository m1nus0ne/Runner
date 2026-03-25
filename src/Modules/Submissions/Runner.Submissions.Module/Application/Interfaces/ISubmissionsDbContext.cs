using Microsoft.EntityFrameworkCore;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Application.Interfaces;

public interface ISubmissionsDbContext
{
    DbSet<Assignment> Assignments { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<CheckResult> CheckResults { get; }
    DbSet<TestGroupResult> TestGroupResults { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<UserProfile> UserProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

