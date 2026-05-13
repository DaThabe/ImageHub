using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 资源
/// </summary>
internal sealed class Resource : AggregateRoot<ResourceId>
{
    /// <summary>
    /// 所属元数据
    /// </summary>
    public MetadataId MetadataId { get; }

    /// <summary>
    /// 来源网址
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// 本地路径
    /// </summary>
    public required string FilePath { get; init; }

    /// <summary>
    /// 顺序
    /// </summary>
    public required int OrderIndex { get; init; }


    private Resource()
    {
        MetadataId = default!;
    }
    public Resource(ResourceId id, MetadataId metadataId) : base(id)
    {
        MetadataId = metadataId;
    }
}