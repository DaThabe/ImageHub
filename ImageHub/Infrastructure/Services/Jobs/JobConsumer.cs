using ImageHub.Application.Jobs;
using ImageHub.Domain.Repositories;
using ImageHub.Enums;
using ImageHub.Events;
using ImageHub.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace ImageHub.Infrastructure.Services.Jobs;


/// <summary>
/// 处理任务
/// </summary>
/// <param name="scopeFactory"></param>
/// <param name="logger"></param>
internal sealed class JobConsumer(
    IServiceScopeFactory scopeFactory, 
    ILogger<JobConsumer> logger
    ) : BackgroundService, IDomainEventHandler<JobCreatedDomainEvent>
{
    private readonly Channel<JobId> _jobChannel = Channel.CreateBounded<JobId>(50);

    public async Task HandleAsync(JobCreatedDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await _jobChannel.Writer.WriteAsync(@event.JobId, cancellationToken);
        logger.LogDebug("已添加至任务消费列表, 任务Id:{id}", @event.JobId);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = stoppingToken
        };

        await Parallel.ForEachAsync(_jobChannel.Reader.ReadAllAsync(stoppingToken), options, async (id, ct) =>
        {
            try
            {
                await ConsumeJobAsync(id, ct);
            }
            catch (OperationCanceledException ex)
            {
                logger.LogInformation(ex, "任务消费者已取消");
            }
            catch (Exception ex)
            {
                //TODO: 先忽略, 以后再重试

                logger.LogError(ex, "任务处理失败, Id:{id}", id);

                // 重新写入
                //await _jobChannel.Writer.WriteAsync(i, stoppingToken);
            }
        });
    }

    private async Task ConsumeJobAsync(JobId jobId, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var job_repository = services.GetRequiredService<IJobRepository>();
        var job_processor = services.GetRequiredService<IJobProcessor>();


        var job = await job_repository.FindByIdAsync(jobId, cancellationToken);
        if (job is null)
        {
            logger.LogWarning("任务已创建, 但任务信息不存在, 任务Id:{id}", jobId);
            return;
        }

        // 任务完成
        if (job.State == JobState.Completed) return;

        // 处理任务
        await job_processor.ProcessAsync(job, cancellationToken);
    }
}
