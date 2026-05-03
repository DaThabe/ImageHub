using ImageHub.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ImageHub.Infrastructure.Database.Configurations;


internal sealed class MetadataEntityConfiguration : IEntityTypeConfiguration<Metadata>
{
    public void Configure(EntityTypeBuilder<Metadata> builder)
    {
        builder.ToTable("Metadata");
        builder.HasKey(e => e.Id);

        // Id 转换
        builder.Property(e => e.Id)
            .HasConversion(ValueConverters.MetadataIdToGuid)
            .ValueGeneratedNever();

        // 任务Id 转换
        builder.Property(e => e.SourceId)
            .HasConversion(ValueConverters.SourceIdToGuid);

        // 任务-元数据 1:1
        builder.HasOne<Source>()
             .WithOne()
             .HasForeignKey<Metadata>(e => e.SourceId)
             .OnDelete(DeleteBehavior.Cascade);

        //TODO: 这里不使用字段是否会映射成功
        // 资源
        builder.Property(e => e.Resources)
            .HasField("_resources");

        // 标签
        builder.Property(e => e.Tags)
            .HasField("_tags");
    }
}
