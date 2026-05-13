using ImageHub.Enums;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;
using ThabeSoft.Mediator;

namespace ImageHub.Domain.Events;


/// <summary>
/// 任务准备就绪, 可以推送
/// </summary>
public sealed record JobResourcesReadyDomainEvent : INotification, IDomainEvent
{
    /// <summary>
    /// 任务Id
    /// </summary>
    public required JobId JobId { get; init; }
    /// <summary>
    /// 创建时间
    /// </summary>
    public required DateTimeOffset CreateAt { get; init; }

    /// <summary>
    /// 来源 Id
    /// </summary>
    public required SourceId SourceId { get; init; }
    /// <summary>
    /// 来源类型
    /// </summary>
    public required SourceType SourceType { get; init; }
    /// <summary>
    /// 来源网址
    /// </summary>
    public required string SourceUrl { get; init; }


    /// <summary>
    /// 元数据Id
    /// </summary>
    public required MetadataId MetadataId { get; init; }
    /// <summary>
    /// 标题
    /// </summary>
    public string? Title { get; init; }
    /// <summary>
    /// 作者名称
    /// </summary>
    public string? AuthorName { get; init; }
    /// <summary>
    /// 作者主页
    /// </summary>
    public string? AuthorUrl { get; init; }
    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; init; }
    /// <summary>
    /// 上传时间
    /// </summary>
    public DateTimeOffset? UploadAt { get; init; }
    /// <summary>
    /// 标签
    /// </summary>
    public HashSet<string> Tags { get; init; } = [];

    /// <summary>
    /// 资源
    /// </summary>
    public required IReadOnlyDictionary<ResourceId, string> ResourceFilePaths { get; init; }

    /// <summary>
    /// 发布目标Id
    /// </summary>
    public required PublishTargetId PublishTargetId { get; init; }
    /// <summary>
    /// 发布任务Id
    /// </summary>
    public required IReadOnlyCollection< PublishJobId> PublishJobIds { get; init; }
}