using ImageHub.Enums;
using ImageHub.Events;
using ImageHub.Models;
using ImageHub.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using ThabeSoft.DomainDrivenDesign;
using ThabeSoft.Mediator;

namespace ImageHub.Infrastructure.Services.Jobs;


/// <summary>
/// 任务队列
/// </summary>
public interface IJobQueue
{
    /// <summary>
    /// 添加任务
    /// </summary>
    ValueTask EnqueueAsync(JobId jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复未完成的任务
    /// </summary>
    ValueTask RecoverAsync(CancellationToken cancellationToken = default);
}


internal sealed class JobQueue(IServiceScopeFactory scopeFactory, ILogger<JobQueue> logger) :
    BackgroundService, IJobQueue, INotificationHandler<JobCreatedDomainEvent>
{
    private readonly Channel<JobId> _channel = Channel.CreateBounded<JobId>(50);

    public async ValueTask EnqueueAsync(JobId jobId, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(jobId, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("已添加至任务消费列表, 任务Id:{id}", jobId);
        }
    }

    public ValueTask HandleAsync(JobCreatedDomainEvent notification, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(notification.JobId, cancellationToken);
    }

    public async ValueTask RecoverAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IDomainEventPublisher>();

        logger.LogDebug("正在从数据库恢复未完成的发布任务");

        try
        {
            // 这里建议设置一个合理的超时时间，防止数据库卡死导致程序启动缓慢
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var jobs = await jobRepository.GetActivitysAsync(cts.Token);

            foreach (var i in jobs)
            {
                await EnqueueAsync(i.Id, cancellationToken);
            }

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("任务恢复完成, 共 {n} 条", jobs.Count);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("恢复任务超时，部分任务可能稍后通过轮询机制加载");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动时加载发布任务失败，请检查数据库连接");
        }
    }

    // 后台循环处理
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 恢复任务
        //await RecoverAsync(stoppingToken);

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = 3,
            CancellationToken = stoppingToken
        };

        await Parallel.ForEachAsync(_channel.Reader.ReadAllAsync(stoppingToken), options, async (id, ct) =>
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