using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database.Configurations;

internal sealed class CheckResultConfiguration : IEntityTypeConfiguration<CheckResult>
{
    public void Configure(EntityTypeBuilder<CheckResult> builder)
    {
        builder.ToTable("check_results");
        builder.HasKey(cr => cr.Id);
        builder.Property(cr => cr.TotalTests).IsRequired();
        builder.Property(cr => cr.PassedTests).IsRequired();
        builder.Property(cr => cr.FailedTests).IsRequired();
        builder.Property(cr => cr.RawNUnitXml).IsRequired();

        builder.HasMany(cr => cr.TestGroupResults)
               .WithOne(tg => tg.CheckResult)
               .HasForeignKey(tg => tg.CheckResultId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

