using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database.Configurations;

internal sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("submissions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StudentId).HasMaxLength(100).IsRequired();
        builder.Property(s => s.GitHubUrl).HasMaxLength(500).IsRequired();
        builder.Property(s => s.Branch).HasMaxLength(250).IsRequired();
        builder.Property(s => s.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(s => s.CreatedAt).IsRequired();
        builder.Property(s => s.GitLabPipelineId);

        builder.HasOne(s => s.CheckResult)
               .WithOne(cr => cr.Submission)
               .HasForeignKey<CheckResult>(cr => cr.SubmissionId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

