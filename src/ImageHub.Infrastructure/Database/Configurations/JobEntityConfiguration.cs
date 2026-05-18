using ImageHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;

/// <summary>
/// 任务
/// </summary>
internal sealed class JobEntityConfiguration : IEntityTypeConfiguration<Job>
{
    public void Configure(EntityTypeBuilder<Job> builder)
    {
        builder.ToTable("Job");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.JobIdToGuid);

        builder.Property(e => e.SourceId)
            .HasConversion(ValueConverters.SourceIdToGuid);

        builder.Property(e => e.State)
            .HasConversion<string>();
    }
}