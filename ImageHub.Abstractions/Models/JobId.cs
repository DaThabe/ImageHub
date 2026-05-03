using StronglyTypedIds;

namespace ImageHub.Models;

/// <summary>
/// 任务 Id
/// </summary>
[StronglyTypedId(Template.Guid)]
public readonly partial struct JobId
{
    public static JobId Create() => new(Guid.CreateVersion7());
}