using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskTracker.Domain.Entities;

namespace TaskTracker.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> b)
    {
        b.ToTable("Tasks");
        b.HasKey(t => t.Id);

        b.Property(t => t.Title).IsRequired().HasMaxLength(200);
        b.Property(t => t.Description).HasMaxLength(2000);
        b.Property(t => t.Status).IsRequired().HasConversion<int>();
        b.Property(t => t.CreatedAtUtc).IsRequired();
        b.Property(t => t.CompletedAtUtc);

        b.HasMany(t => t.Tags)
            .WithMany(tag => tag.Tasks)
            .UsingEntity(j => j.ToTable("TaskTags"));

        b.HasIndex(t => new { t.ProjectId, t.Status });
    }
}
