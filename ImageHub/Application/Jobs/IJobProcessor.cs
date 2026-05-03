using ImageHub.Application.Metadatas;
using ImageHub.Domain.Entities;
using ImageHub.Domain.Events;
using ImageHub.Domain.Repositories;
using ImageHub.Enums;
using ImageHub.Events;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Models;
using ImageHub.Services;
using Microsoft.Extensions.Logging;

namespace ImageHub.Application.Jobs;


/// <summary>
/// 任务处理器
/// </summary>
public interface IJobProcessor
{
    /// <summary>
    /// 处理
    /// </summary>
    /// <param name="job"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task ProcessAsync(Job job, CancellationToken cancellationToken = default);
}

/// <summary>
/// 任务处理器
/// </summary>
public sealed class JobProcessor(
    IUnitOfWork unitOfWork,

    IJobRepository jobRepository,
    ISourceRepository sourceRepository,

    IMetadataRepository metadataRepository,
    IMetadataOrchestrator metadataOrchestrator,

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
        Metadata metadata = await GetOrDownloadMetadata( source, cancellationToken);
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

        foreach(var url in metadata.Resources)
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
            catch(Exception ex)
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

            if (publish_jobs.Count <= 0) continue;
            all_publish_jobs[publish_target.Type] = (publish_target, publish_jobs);
        }

        // 发布事件
        foreach(var i in all_publish_jobs)
        {
            var target = i.Value.Target;
            var job_ids = i.Value.Jobs.Select(x=>x.Id).ToArray();
            

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
            await domainEventPublisher.PublsihAsync(@event, cancellationToken);
            // 标记完成
            await publishJobService.MarkCompletedAsync(job_ids, cancellationToken);
        }

        return false;
    }
}


///// <summary>
///// 任务已创建事件处理器
///// </summary>
//public sealed class JobCreatedDomainEventHandler(
//    IUnitOfWork unitOfWork,
//    IJobRepository jobRepository,
//    ISourceRepository sourceRepository,
//    IMetadataRepository metadataRepository,
//    IMetadataOrchestrator metadataOrchestrator,
//    IDomainEventPublisher domainEventPublisher,
//    ILogger<JobCreatedDomainEventHandler> logger
//    ) : IDomainEventHandler<JobCreatedDomainEvent>
//{
//    public async Task HandleAsync(JobCreatedDomainEvent @event, CancellationToken cancellationToken = default)
//    {
//        var job = await jobRepository.FindByIdAsync(@event.JobId, cancellationToken) 
//            ?? throw new InvalidOperationException("任务不存在");

//        var source = await sourceRepository.FindByIdAsync(@event.SourceId, cancellationToken)
//            ?? throw new InvalidOperationException("来源不存在");

//        // 开始下载
//        job.StartDownloadMetadata();
//        await jobRepository.UpdateAsync(job, cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);


//        using var _ = logger.BeginScope(new Dictionary<string, object> { ["SourceId"] = @event.SourceId });

//        // 根据 SourceId 查找元数据，如果存在则直接返回
//        var metadata = await metadataRepository.FindBySourceIdAsync(@event.SourceId, cancellationToken);
//        if (metadata is not null) return;

//        // 下载元数据
//        metadata = await metadataOrchestrator.FetchAsync(source, cancellationToken);
//        await metadataRepository.AddAsync(metadata, cancellationToken);
//        job.MetadataDownloaded();
//        await jobRepository.UpdateAsync(job, cancellationToken);

//        // 提交下载完成阶段数据
//        await unitOfWork.SaveChangesAsync(cancellationToken);

//        var new_event = new MetadataDownloadedDomainEvent(@event.JobId, @event.SourceId, metadata.Id);
//        await domainEventPublisher.PublsihAsync(new_event, cancellationToken);
//    }
//}

//// 元数据下载完成事件处理
//public sealed class MetadataDownloadedDomainEventHandler(
//    IUnitOfWork unitOfWork,
//    IJobRepository jobRepository,
//    ISourceRepository sourceRepository,
//    IMetadataRepository metadataRepository,
//    IResourceRepository resourceRepository,
//    IResourceDownloader resourceDownloader,
//    IDomainEventPublisher domainEventPublisher,
//    ILogger<JobCreatedDomainEventHandler> logger
//    ) : IDomainEventHandler<MetadataDownloadedDomainEvent>
//{
//    public async Task HandleAsync(MetadataDownloadedDomainEvent @event, CancellationToken cancellationToken = default)
//    {
//        var job = await jobRepository.FindByIdAsync(@event.JobId, cancellationToken)
//            ?? throw new InvalidOperationException("任务不存在");

//        var source = await sourceRepository.FindByIdAsync(@event.SourceId, cancellationToken)
//            ?? throw new InvalidOperationException("来源不存在");

//        var metadata = await metadataRepository.FindByIdAsync(@event.MetadataId, cancellationToken)
//            ?? throw new InvalidOperationException("元数据不存在");


//        // 开始下载
//        job.StartDownloadResources();
//        await jobRepository.UpdateAsync(job, cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);

//        // 下载资源
//        int order_index = 0;
//        List<Resource> resources = [];

//        foreach (var url in metadata.Resources)
//        {
//            try
//            {
//                var file_path = await resourceDownloader.DownloadAsync(url, source.Type, true, cancellationToken);
//                var resource = await resourceRepository.FindByUrlAsync(url, cancellationToken);

//                // 如果不存在则保存
//                if (resource is null)
//                {
//                    resource = new Resource(ResourceId.Create(), metadata.Id) { Url = url, FilePath = file_path, OrderIndex = order_index++ };
//                    await resourceRepository.AddAsync(resource, cancellationToken);
//                }

//                resources.Add(resource);
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, "资源下载失败, 网址:{url}", url);

//                //TODO: 先失败, 以后再重试
//                job.Fail();
//                await jobRepository.UpdateAsync(job, cancellationToken);
//                await unitOfWork.SaveChangesAsync(cancellationToken);
//                throw;
//            }
//        }

//        // 提交下载完成阶段数据
//        job.ResourceDownloaded();
//        await jobRepository.UpdateAsync(job, cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);

//        var new_event = new ResourceDownloadedDomainEvent(@event.JobId, @event.SourceId, @event.MetadataId, )
//        await domainEventPublisher.PublsihAsync(new_event, cancellationToken);
//    }
//}

//// 资源下载完成事件处理
//public sealed class ResourceDownloadedDomainEventHandler(
//    IUnitOfWork unitOfWork,
//    IJobRepository jobRepository,
//    ISourceRepository sourceRepository,
//    IMetadataRepository metadataRepository,
//    IPublishJobService publishJobService,
//    IPublishJobRepository publishJobRepository,
//    IPublishTargetRepository publishTargetRepository,
//    IDomainEventPublisher domainEventPublisher,
//    ILogger<JobCreatedDomainEventHandler> logger
//    ) : IDomainEventHandler<ResourceDownloadedDomainEvent>
//{
//    public async Task HandleAsync(ResourceDownloadedDomainEvent @event, CancellationToken cancellationToken = default)
//    {
//        var job = await jobRepository.FindByIdAsync(@event.JobId, cancellationToken)
//            ?? throw new InvalidOperationException("任务不存在");

//        var source = await sourceRepository.FindByIdAsync(@event.SourceId, cancellationToken)
//            ?? throw new InvalidOperationException("来源不存在");

//        var metadata = await metadataRepository.FindByIdAsync(@event.MetadataId, cancellationToken)
//            ?? throw new InvalidOperationException("元数据不存在");


//        // 开始下载
//        job.StartPublish();
//        await jobRepository.UpdateAsync(job, cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);


//        // 所有推送任务
//        Dictionary<PublishTargetType, (PublishTarget Target, List<PublishJob> Jobs)> all_publish_jobs = [];
//        // 所有推送目标
//        var targets = await publishTargetRepository.GetAllAsync(cancellationToken);

//        foreach (var publish_target in targets)
//        {
//            List<PublishJob> publish_jobs = [];

//            foreach (var resource_id in @event.ResourceIds)
//            {
//                var publish_job = new PublishJob(PublishJobId.Create(), job.Id, source.Id, metadata.Id, resource_id, publish_target.Id);
//                publish_jobs.Add(publish_job);
//            }

//            all_publish_jobs[publish_target.Type] = (publish_target, publish_jobs);
//        }

//        // 批量保存
//        await publishJobRepository.AddRangeAsync(all_publish_jobs.Values.SelectMany(x => x.Jobs), cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);



//        // 发布事件
//        foreach (var i in all_publish_jobs)
//        {
//            var target = i.Value.Target;
//            var job_ids = i.Value.Jobs.Select(x => x.Id).ToArray();

//            //var @event = new JobResourcesReadyDomainEvent()
//            //{
//            //    JobId = job.Id,
//            //    CreateAt = job.CreateAt,

//            //    SourceId = source.Id,
//            //    SourceType = source.Type,
//            //    SourceUrl = source.Url,

//            //    MetadataId = metadata.Id,
//            //    AuthorName = metadata.AuthorName,
//            //    AuthorUrl = metadata.AuthorUrl,
//            //    Title = metadata.Title,
//            //    Description = metadata.Description,
//            //    UploadAt = metadata.UploadAt,

//            //    ResourceFilePaths = resources.OrderBy(x => x.OrderIndex).Select(x => (x.Id, x.FilePath)).ToDictionary(k => k.Id, v => v.FilePath),

//            //    PublishTargetId = target.Id,
//            //    PublishJobIds = job_ids
//            //};



//            // 标记完成
//            await publishJobService.MarkCompletedAsync(job_ids, cancellationToken);
//            await unitOfWork.SaveChangesAsync(cancellationToken);

//            var new_event = new PublishedDomainEvent(@event.JobId, @event.SourceId, @event.MetadataId, @event.ResourceIds, target.Id, job_ids);
//            await domainEventPublisher.PublsihAsync(@event, cancellationToken);
//        }

//        // 提交下载完成阶段数据
//        job.Published();
//        job.Complete();
//        await jobRepository.UpdateAsync(job, cancellationToken);
//        await unitOfWork.SaveChangesAsync(cancellationToken);
//    }
//}
