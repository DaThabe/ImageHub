using ImageHub.Enums;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 发布任务
/// </summary>
public sealed class PublishJob : AggregateRoot<PublishJobId>
{
    public JobId JobId { get; }
    public SourceId SourceId { get; }
    public MetadataId MetadataId { get; }
    public ResourceId ResourceId { get; }
    public PublishTargetId PublishTargetId { get; }

    public DateTimeOffset CreatedAt { get; } = DateTimeOffset.UtcNow;
    public PublishJobState State { get; private set; } = PublishJobState.Pending;

    public int RetryCount { get; } = 3;
    public int CurrentRetryCount { get; }


    private PublishJob()
    {

    }
    public PublishJob(PublishJobId id, JobId jobId, SourceId sourceId, MetadataId metadataId, ResourceId resourceId, PublishTargetId publishTargetId) : base(id)
    {
        JobId = jobId;
        SourceId = sourceId;
        MetadataId = metadataId;
        ResourceId = resourceId;
        PublishTargetId = publishTargetId;
    }


    public void Completed()
    {
        if (State != PublishJobState.Pending)
        {
            throw new InvalidOperationException($"当前状态 {State} 无法完成");
        }
        State = PublishJobState.Completed;
    }
    public void Failed()
    {
        if (State == PublishJobState.Completed)
        {
            throw new InvalidOperationException($"当前状态 {State} 无法异常");
        }

        State = PublishJobState.Failed;
    }
}