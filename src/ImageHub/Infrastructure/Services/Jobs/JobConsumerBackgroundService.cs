using ImageHub.Application.Services;
using ImageHub.Domain.Repositories;
using ImageHub.Enums;
using ImageHub.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Services.Jobs;

/// <summary>
/// 任务消费者
/// </summary>
internal sealed class JobConsumerBackgroundService(IServiceScopeFactory scopeFactory, ILogger<JobConsumerBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var job_queue = services.GetRequiredService<IJobQueue>();

        // 恢复任务
        await job_queue.RecoverAsync(stoppingToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = stoppingToken
        };

        await Parallel.ForEachAsync(job_queue.DequeueAsync(stoppingToken), options, async (id, ct) =>
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

    // 消费任务 Id
    private async Task ConsumeJobAsync(JobId jobId, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
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
