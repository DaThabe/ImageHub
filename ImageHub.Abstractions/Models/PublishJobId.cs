using StronglyTypedIds;

namespace ImageHub.Models;

/// <summary>
/// 发布任务Id
/// </summary>
[StronglyTypedId(Template.Guid)]
public readonly partial struct PublishJobId
{
    public static PublishJobId Create() => new(Guid.CreateVersion7());
}
