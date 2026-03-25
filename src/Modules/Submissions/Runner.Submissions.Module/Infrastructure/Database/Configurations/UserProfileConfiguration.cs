using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Runner.Submissions.Module.Domain.Entities;

namespace Runner.Submissions.Module.Infrastructure.Database.Configurations;

internal sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("user_profiles");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.GitHubLogin).HasMaxLength(100).IsRequired();
        builder.Property(u => u.GitHubId).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(u => u.GitHubId).IsUnique();
        builder.HasIndex(u => u.GitHubLogin).IsUnique();
    }
}

