using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database.Configurations;

internal sealed class TestGroupResultConfiguration : IEntityTypeConfiguration<TestGroupResult>
{
    public void Configure(EntityTypeBuilder<TestGroupResult> builder)
    {
        builder.ToTable("test_group_results");
        builder.HasKey(tg => tg.Id);
        builder.Property(tg => tg.GroupName).HasMaxLength(200).IsRequired();
        builder.Property(tg => tg.ErrorType).HasConversion<string>().HasMaxLength(50);
        builder.Property(tg => tg.ErrorMessage).HasMaxLength(2000);
    }
}

