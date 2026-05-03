using ImageHub.Enums;
using ImageHub.Events;
using ImageHub.Models;

namespace ImageHub.Domain.Entities;

/// <summary>
/// 任务
/// </summary>
public sealed class Job : AggregateRoot<JobId>
{
    public SourceId SourceId { get; }
    public JobState State { get; private set; } = JobState.Pending;
    public DateTimeOffset CreateAt { get; private set; } = DateTimeOffset.UtcNow;

    private Job()
    {

    }
    public Job(JobId id, SourceId sourceId) : base(id)
    {
        SourceId = sourceId;
        AddDomainEvent(new JobCreatedDomainEvent(id, sourceId));
    }


    public void StartDownloadMetadata()
    {
        if (State != JobState.Pending)
        {
            throw new InvalidOperationException($"无法从 {State} 状态下载元数据");
        }
        State = JobState.MetadataDownloading;
    }
    public void MetadataDownloaded()
    {
        if (State != JobState.MetadataDownloading)
        {
            throw new InvalidOperationException($"无法从 {State} 状态完成元数据下载");
        }
        State = JobState.MetadataDownloaded;
    }
    
    public void StartDownloadResources()
    {
        if (State != JobState.MetadataDownloaded)
        {
            throw new InvalidOperationException($"无法从 {State} 状态开始下载资源");
        }
        State = JobState.ResourceDownloading;
    }
    public void ResourceDownloaded()
    {
        if (State != JobState.ResourceDownloading)
        {
            throw new InvalidOperationException($"无法从 {State} 状态开始下载资源");
        }
        State = JobState.ResourceDownloaded;
    }

    public void StartPublish()
    {
        if (State != JobState.ResourceDownloaded)
        {
            throw new InvalidOperationException($"无法从 {State} 状态开始推送");
        }
        State = JobState.Publishing;
    }
    public void Published()
    {
        if (State != JobState.Publishing)
        {
            throw new InvalidOperationException($"无法从 {State} 状态推送完毕");
        }
        State = JobState.Published;
    }

    public void Complete()
    {
        if (State != JobState.Published)
        {
            throw new InvalidOperationException($"无法从 {State} 状态完成任务");
        }

        State = JobState.Completed;
    }
    public void Fail()
    {
        if (State == JobState.Completed)
        {
            throw new InvalidOperationException("已完成的任务不能标记为失败");
        }

        State = JobState.Failed;
    }
}