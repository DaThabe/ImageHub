using ImageHub.Application.Services;
using ImageHub.Domain.Entities;
using ImageHub.Infrastructure.Repositories;
using ImageHub.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Services.Jobs;

internal sealed class JobQueue(IServiceScopeFactory scopeFactory, ILogger<JobQueue> logger) : IJobQueue
{
    private readonly Channel<JobId> _channel = Channel.CreateBounded<JobId>(50);

    public IAsyncEnumerable<JobId> DequeueAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }

    public async ValueTask EnqueueAsync(JobId jobId, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(jobId, cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("已添加至任务消费列表, 任务Id:{id}", jobId);
        }
    }

    public async ValueTask RecoverAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IRepository<Job, JobId>>();
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
}