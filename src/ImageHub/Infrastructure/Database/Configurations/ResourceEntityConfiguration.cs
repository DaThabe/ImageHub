using ImageHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;

internal sealed class ResourceEntityConfiguration : IEntityTypeConfiguration<Resource>
{
    public void Configure(EntityTypeBuilder<Resource> builder)
    {
        builder.ToTable("Resource");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.ResourceIdToGuid);

        builder.Property(e => e.MetadataId)
            .HasConversion(ValueConverters.MetadataIdToGuid)
            .ValueGeneratedNever();
    }
}
