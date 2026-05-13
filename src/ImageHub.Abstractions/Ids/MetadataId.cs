using StronglyTypedIds;

namespace ImageHub.Models;

/// <summary>
/// 元数据 Id
/// </summary>
/// <param name="Value"></param>
[StronglyTypedId(Template.Guid)]
public readonly partial struct MetadataId
{
    public static MetadataId Create() => new(Guid.CreateVersion7());
}