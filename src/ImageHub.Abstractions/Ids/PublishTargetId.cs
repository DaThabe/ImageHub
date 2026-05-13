using StronglyTypedIds;

namespace ImageHub.Models;

/// <summary>
/// 发布目标Id
/// </summary>
[StronglyTypedId(Template.Guid)]
public readonly partial struct PublishTargetId
{
    public static PublishTargetId Create() => new(Guid.CreateVersion7());
}