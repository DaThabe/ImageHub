using ImageHub.Enums;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 发布目标
/// </summary>
public abstract class PublishTarget : AggregateRoot<PublishTargetId>
{
    public PublishTargetType Type { get; }


    protected PublishTarget()
    {

    }
    protected PublishTarget(PublishTargetId id, PublishTargetType type) : base(id)
    {
        Type = type;
    }
}


/// <summary>
/// 电报群组
/// </summary>
public sealed class TelegramGroupPublishTarget : PublishTarget
{
    public long GroupId { get; }


    private TelegramGroupPublishTarget()
    {

    }
    public TelegramGroupPublishTarget(PublishTargetId id, long groupId) : base(id, PublishTargetType.TelegramGroup)
    {
        GroupId = groupId;
    }
}