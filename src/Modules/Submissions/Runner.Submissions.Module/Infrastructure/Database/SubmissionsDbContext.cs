using Microsoft.EntityFrameworkCore;
using Runner.Submissions.Module.Application.Interfaces;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database;

internal sealed class SubmissionsDbContext(DbContextOptions<SubmissionsDbContext> options)
    : DbContext(options), ISubmissionsDbContext
{
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<CheckResult> CheckResults => Set<CheckResult>();
    public DbSet<TestGroupResult> TestGroupResults => Set<TestGroupResult>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("submissions");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SubmissionsDbContext).Assembly);
    }
}

