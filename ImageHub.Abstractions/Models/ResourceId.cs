using StronglyTypedIds;

namespace ImageHub.Models;


/// <summary>
/// 资源 Id
/// </summary>
[StronglyTypedId(Template.Guid)]
public readonly partial struct ResourceId
{
    public static ResourceId Create() => new(Guid.CreateVersion7());
}
