using ImageHub.Models;

namespace ImageHub.Services;

/// <summary>
/// 发布任务
/// </summary>
public interface IPublishJobService
{
    /// <summary>
    /// 标记为推送完成
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask MarkCompletedAsync(PublishJobId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记为推送完成
    /// </summary>
    /// <param name="ids"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    ValueTask MarkCompletedAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default);
}