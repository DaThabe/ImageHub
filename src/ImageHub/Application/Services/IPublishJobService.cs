using ImageHub.Models;

namespace ImageHub.Application.Services;


/// <summary>
/// 发布任务
/// </summary>
public interface IPublishJobService
{
    /// <summary>
    /// 标记为推送完成
    /// </summary>
    ValueTask MarkCompletedAsync(PublishJobId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 标记为推送完成
    /// </summary>
    ValueTask MarkCompletedAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default);
}
