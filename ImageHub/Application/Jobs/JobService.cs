using Flurl;
using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Domain.Services;
using ImageHub.Models;
using ImageHub.Services;

namespace ImageHub.Application.Jobs;


public record CreateJob(string Url);
public record CreateJobResult(JobId JobId);

public sealed class CreateJobHandler(
    ISourceParser sourceService,
    ISourceRepository sourceRepository,
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork
    )
{
    public async Task<CreateJobResult> HandleAsync(CreateJob createJob, CancellationToken cancellationToken = default)
    {
        // 获取来源
        var source = sourceService.Parse(createJob.Url);
        await sourceRepository.UpsertAsync(source, cancellationToken);

        // 创建任务
        var job = await jobRepository.GetOrCreateBySourceIdAsync(source.Id, cancellationToken);

        // 保存修改
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateJobResult(job.Id);
    }
}


/// <summary>
/// 图像中心引擎
/// </summary>
internal sealed class JobService(
    ISourceParser sourceService,
    ISourceRepository sourceRepository,
    IJobRepository jobRepository,
    IUnitOfWork unitOfWork
    ) : IJobService
{
    public async Task<JobId> CreateAsync(string url, CancellationToken cancellationToken = default)
    {
        var source = await GetOrCreateSource(url, cancellationToken);
        var job = await GetOrCreateJobAsync(source.Id, cancellationToken);
        return job.Id;
    }


    // 获取或创建来源
    private async Task<Source> GetOrCreateSource(string url, CancellationToken cancellationToken)
    {
        if (!sourceService.TryParse(url, out var source))
        {
            throw new InvalidOperationException("无法识别的来源");
        }

        if (await sourceRepository.ExistsAsync(source.Id, cancellationToken))
        {
            return source;
        }

        await sourceRepository.AddAsync(source, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return source;
    }

    // 获取或创建任务
    private async Task<Job> GetOrCreateJobAsync(SourceId sourceId, CancellationToken cancellationToken)
    {
        var job = await jobRepository.FindBySourceIdAsync(sourceId, cancellationToken);
        if (job is not null) return job;

        job = new Job(JobId.Create(), sourceId);
        
        await jobRepository.AddAsync(job, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        
        return job;
    }
}
