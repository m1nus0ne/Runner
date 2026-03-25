using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database.Configurations;

internal sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("assignments");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).HasMaxLength(200).IsRequired();
        builder.Property(a => a.GitLabProjectId).IsRequired();
        builder.Property(a => a.Type).HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.CoverageThreshold);
        builder.Property(a => a.TemplateRepoUrl).HasMaxLength(500);

        builder.HasMany(a => a.Submissions)
               .WithOne(s => s.Assignment)
               .HasForeignKey(s => s.AssignmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

