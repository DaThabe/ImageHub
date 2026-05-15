using ImageHub.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;

internal sealed class PublishJobEntityConfiguration : IEntityTypeConfiguration<PublishJob>
{
    public void Configure(EntityTypeBuilder<PublishJob> builder)
    {
        builder.ToTable("PublishJob");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.PublishJobIdToGuid)
            .ValueGeneratedNever();

        builder.Property(e => e.JobId)
            .HasConversion(ValueConverters.JobIdToGuid)
            .ValueGeneratedNever();

        builder.Property(e => e.MetadataId)
           .HasConversion(ValueConverters.MetadataIdToGuid)
           .ValueGeneratedNever();

        builder.Property(e => e.SourceId)
            .HasConversion(ValueConverters.SourceIdToGuid);

        builder.Property(e => e.ResourceId)
            .HasConversion(ValueConverters.ResourceIdToGuid);

        builder.Property(e => e.PublishTargetId)
            .HasConversion(ValueConverters.PublishTargetIdToGuid);

        builder.Property(e => e.State)
            .HasConversion<string>();
    }
}
