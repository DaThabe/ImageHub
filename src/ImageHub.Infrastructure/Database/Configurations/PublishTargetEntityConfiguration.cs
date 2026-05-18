using ImageHub.Entities;
using ImageHub.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;

internal sealed class PublishTargetEntityConfiguration : 
    IEntityTypeConfiguration<PublishTarget>,
    IEntityTypeConfiguration<TelegramGroupPublishTarget>
{
    public void Configure(EntityTypeBuilder<PublishTarget> builder)
    {
        builder.ToTable("PublishTarget");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.PublishTargetIdToGuid);

        builder.HasDiscriminator<string>("PublishTargetType")
            .HasValue<TelegramGroupPublishTarget>(nameof(PublishTargetType.TelegramGroup));

        builder.Ignore(e => e.Type);
    }

    public void Configure(EntityTypeBuilder<TelegramGroupPublishTarget> builder)
    {
        builder.Property(p => p.GroupId);
    }
}