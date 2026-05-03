using ImageHub.Domain.Events;
using ImageHub.Domain.Repositories;
using ImageHub.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Services.Jobs;


/// <summary>
/// 任务恢复后台任务
/// </summary>
/// <param name="publishJobService"></param>
/// <param name="logger"></param>
internal sealed class JobRecoveryBackgroundService(
    IServiceScopeFactory scopeFactory,
    IDomainEventPublisher domainEventPublisher, 
    ILogger<JobRecoveryBackgroundService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var jobRepository = scope.ServiceProvider.GetRequiredService<IJobRepository>();


        logger.LogDebug("正在从数据库恢复未完成的发布任务...");

        try
        {
            // 这里建议设置一个合理的超时时间，防止数据库卡死导致程序启动缓慢
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            var jobs = await jobRepository.FindActivitysAsync(cts.Token);

            foreach (var i in jobs)
            {
                var @event = new JobCreatedDomainEvent(i.Id, i.SourceId);
                await domainEventPublisher.PublsihAsync(@event, cancellationToken);
            }

            logger.LogInformation("任务恢复完成, 共 {n} 条", jobs.Count);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("恢复任务超时，部分任务可能稍后通过轮询机制加载。");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "启动时加载发布任务失败，请检查数据库连接。");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}