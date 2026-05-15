using ImageHub.Application.Services;
using ImageHub.Domain.Events;
using ImageHub.Entities;
using ImageHub.Enums;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Models;
using ImageHub.Repositories;
using Microsoft.Extensions.Logging;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Services.Jobs;


/// <summary>
/// 任务处理器
/// </summary>
public interface IJobProcessor
{
    /// <summary>
    /// 处理
    /// </summary>
    Task ProcessAsync(Job job, CancellationToken cancellationToken = default);
}

/// <summary>
/// 任务处理器
/// </summary>
internal sealed class JobProcessor(
    IUnitOfWork unitOfWork,

    IJobRepository jobRepository,
    ISourceRepository sourceRepository,

    IMetadataRepository metadataRepository,
    IMetadataProvider metadataOrchestrator,

    IResourceDownloader resourceService,
    IResourceRepository resourceRepository,

    IPublishJobService publishJobService,
    IPublishJobRepository publishJobRepository,
    IPublishTargetRepository publishTargetRepository,

    IDomainEventPublisher domainEventPublisher,

    ILogger<JobProcessor> logger
    ) : IJobProcessor
{
    public async Task ProcessAsync(Job job, CancellationToken cancellationToken = default)
    {
        // 获取来源
        Source source = await GetSource(job, cancellationToken);
        if (job.State == JobState.Pending)
        {
            job.StartDownloadMetadata();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }


        // 下载元信息
        Metadata metadata = await GetOrDownloadMetadata(source, cancellationToken);
        if (job.State == JobState.MetadataDownloading)
        {
            // 下载完成
            job.MetadataDownloaded();
            job.StartDownloadResources();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 下载资源
        List<Resource> resources = await GetOrDownloadResources(job, source, metadata, cancellationToken);
        if (job.State == JobState.ResourceDownloading)
        {
            // 提交下载完成阶段数据
            job.ResourceDownloaded();
            job.StartPublish();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        // 推送
        await PublishAsync(job, source, metadata, resources, cancellationToken);
        if (job.State == JobState.Publishing)
        {
            // 提交下载完成阶段数据
            job.Published();
            job.Complete();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }


    private async Task<Source> GetSource(Job job, CancellationToken cancellationToken)
    {
        var source = await sourceRepository.FindByIdAsync(job.SourceId, cancellationToken);

        if (source is null)
        {
            job.Fail();
            await jobRepository.UpdateAsync(job, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            throw new InvalidOperationException($"无法获取来源信息, 任务 Id:{job.Id}");
        }

        return source;
    }
    private async Task<Metadata> GetOrDownloadMetadata(Source source, CancellationToken cancellationToken)
    {
        using var _ = logger.BeginScope(new Dictionary<string, object> { ["SourceId"] = source.Id, ["Url"] = source.Url });

        // 根据 SourceId 查找元数据，如果存在则直接返回
        var metadata = await metadataRepository.FindBySourceIdAsync(source.Id, cancellationToken);
        if (metadata is not null) return metadata;

        // 下载元数据
        metadata = await metadataOrchestrator.FetchAsync(source, cancellationToken);


        await metadataRepository.AddAsync(metadata, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return metadata;
    }
    private async Task<List<Resource>> GetOrDownloadResources(Job job, Source source, Metadata metadata, CancellationToken cancellationToken)
    {
        // 下载资源
        int order_index = 0;
        List<Resource> resources = [];

        foreach (var url in metadata.Resources)
        {
            try
            {
                var file_path = await resourceService.DownloadAsync(url, source.Type, true, cancellationToken);
                var resource = await resourceRepository.FindByUrlAsync(url, cancellationToken);

                // 如果不存在则保存
                if (resource is null)
                {
                    resource = new Resource(ResourceId.Create(), metadata.Id) { Url = url, FilePath = file_path, OrderIndex = order_index++ };
                    await resourceRepository.AddAsync(resource, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                resources.Add(resource);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "资源下载失败, 网址:{url}", url);

                //TODO: 先失败, 以后再重试
                job.Fail();
                await jobRepository.UpdateAsync(job, cancellationToken);
                await unitOfWork.SaveChangesAsync(cancellationToken);
                throw;
            }
        }

        return resources;
    }

    private async Task<bool> PublishAsync(Job job, Source source, Metadata metadata, IReadOnlyCollection<Resource> resources, CancellationToken cancellationToken)
    {
        // 所有推送任务
        Dictionary<PublishTargetType, (PublishTarget Target, List<PublishJob> Jobs)> all_publish_jobs = [];
        // 所有推送目标
        var targets = await publishTargetRepository.GetAllAsync(cancellationToken);

        foreach (var publish_target in targets)
        {
            List<PublishJob> publish_jobs = [];

            foreach (var resource in resources)
            {
                var publish_job = await publishJobRepository.FindByTargetIdAndResourceId(publish_target.Id, resource.Id, cancellationToken);
                if (publish_job is null)
                {
                    publish_job = new PublishJob(PublishJobId.Create(), job.Id, source.Id, metadata.Id, resource.Id, publish_target.Id);

                    await publishJobRepository.AddAsync(publish_job, cancellationToken);
                    await unitOfWork.SaveChangesAsync(cancellationToken);
                }

                // 排除完成的任务
                if (publish_job.State == PublishJobState.Completed) continue;
                publish_jobs.Add(publish_job);
            }

            if (publish_jobs.Count == 0) continue;
            all_publish_jobs[publish_target.Type] = (publish_target, publish_jobs);
        }

        // 发布事件
        foreach (var i in all_publish_jobs)
        {
            var target = i.Value.Target;
            var job_ids = i.Value.Jobs.Select(x => x.Id).ToArray();


            var @event = new JobResourcesReadyDomainEvent()
            {
                JobId = job.Id,
                CreateAt = job.CreateAt,

                SourceId = source.Id,
                SourceType = source.Type,
                SourceUrl = source.Url,

                MetadataId = metadata.Id,
                AuthorName = metadata.AuthorName,
                AuthorUrl = metadata.AuthorUrl,
                Title = metadata.Title,
                Description = metadata.Description,
                UploadAt = metadata.UploadAt,

                ResourceFilePaths = resources.OrderBy(x => x.OrderIndex).Select(x => (x.Id, x.FilePath)).ToDictionary(k => k.Id, v => v.FilePath),

                PublishTargetId = target.Id,
                PublishJobIds = job_ids
            };

            // 发布事件
            await domainEventPublisher.PublishAsync(@event, cancellationToken);
            // 标记完成
            await publishJobService.MarkCompletedAsync(job_ids, cancellationToken);
        }

        return false;
    }
}