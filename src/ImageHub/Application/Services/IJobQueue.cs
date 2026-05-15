using ImageHub.Models;

namespace ImageHub.Application.Services;


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

    /// <summary>
    /// 获取任务
    /// </summary>
    IAsyncEnumerable<JobId> DequeueAsync(CancellationToken cancellationToken = default);
}