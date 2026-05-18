using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace ImageHub.Infrastructure.Database;

/// <summary>
/// 数据库迁移业务
/// </summary>
internal sealed class DbMigrateHostedService(IServiceScopeFactory scopeFactory, ILogger<DbMigrateHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ImageHubDbContext>();


        var begin = Stopwatch.GetTimestamp();

        try
        {
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
            var migrations = pendingMigrations.ToList();

            if (migrations.Count == 0)
            {
                logger.LogDebug("数据库版本检查完成：已经是最新。");
                return;
            }

            logger.LogInformation("检测到 {Count} 个待处理的数据库迁移项目，准备开始执行...", migrations.Count);
            logger.LogDebug("待处理项目详情: {Names}", string.Join(", ", migrations));

            await dbContext.Database.MigrateAsync(cancellationToken);

            var elapsed = Stopwatch.GetElapsedTime(begin);
            logger.LogInformation("数据库迁移成功。总数: {Count}, 耗时: {Elapsed}ms", migrations.Count, elapsed);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("数据库迁移任务已被取消（系统正在关闭）。");
        }
        catch (Exception ex)
        {
            var elapsed = Stopwatch.GetElapsedTime(begin);
            logger.LogCritical(ex, "数据库迁移任务由于发生未处理异常而失败！耗时: {Elapsed}ms", elapsed);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
