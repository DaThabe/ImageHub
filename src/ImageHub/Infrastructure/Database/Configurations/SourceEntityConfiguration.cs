using ImageHub.Domain.Entities;
using ImageHub.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;

internal sealed class SourceEntityConfiguration : IEntityTypeConfiguration<Source>,
    IEntityTypeConfiguration<PixivArtworksSource>,
    IEntityTypeConfiguration<TwitterTweetSource>,
    IEntityTypeConfiguration<XiaoHongShuNoteSource>,
    IEntityTypeConfiguration<WeiboBlogSource>
{
    public void Configure(EntityTypeBuilder<Source> builder)
    {
        builder.ToTable("Source");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.SourceIdToGuid)
            .ValueGeneratedNever();

        builder.HasDiscriminator<string>("SourceType")
            .HasValue<PixivArtworksSource>(nameof(SourceType.Pixiv))
            .HasValue<TwitterTweetSource>(nameof(SourceType.Twitter))
            .HasValue<XiaoHongShuNoteSource>(nameof(SourceType.XiaoHongShu))
            .HasValue<WeiboBlogSource>(nameof(SourceType.Weibo));

        builder.Ignore(e => e.Type);
    }

    public void Configure(EntityTypeBuilder<PixivArtworksSource> builder)
    {
        builder.Property(p => p.Pid);
    }

    public void Configure(EntityTypeBuilder<TwitterTweetSource> builder)
    {
        builder.Property(t => t.TweetId);
        builder.Property(t => t.Username).HasMaxLength(100);
    }

    public void Configure(EntityTypeBuilder<XiaoHongShuNoteSource> builder)
    {
        builder.Property(x => x.NoteId).HasMaxLength(64);
        builder.Property(x => x.XsecToken).HasMaxLength(256);
    }

    public void Configure(EntityTypeBuilder<WeiboBlogSource> builder)
    {
        builder.Property(x => x.UserId);
        builder.Property(x => x.BlogId).HasMaxLength(256);
    }
}
