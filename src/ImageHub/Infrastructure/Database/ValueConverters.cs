using ImageHub.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ImageHub.Infrastructure.Database;


/// <summary>
/// 值转换器
/// </summary>
internal static class ValueConverters
{
    public static ValueConverter<JobId, Guid> JobIdToGuid { get; } = new
    (
        id => id.Value,
        value => new JobId(value)
    );
    public static ValueConverter<SourceId, Guid> SourceIdToGuid { get; } = new
    (
        id => id.Value,
        value => new SourceId(value)
    );

    public static ValueConverter<MetadataId, Guid> MetadataIdToGuid { get; } = new
    (
        id => id.Value,
        value => new MetadataId(value)
    );

    public static ValueConverter<ResourceId, Guid> ResourceIdToGuid { get; } = new
    (
        id => id.Value,
        value => new ResourceId(value)
    );

    public static ValueConverter<PublishJobId, Guid> PublishJobIdToGuid { get; } = new
    (
        id => id.Value,
        value => new PublishJobId(value)
    );

    public static ValueConverter<PublishTargetId, Guid> PublishTargetIdToGuid { get; } = new
    (
        id => id.Value,
        value => new PublishTargetId(value)
    );
}
