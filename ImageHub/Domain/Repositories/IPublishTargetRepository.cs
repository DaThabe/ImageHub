using ImageHub.Domain.Entities;
using ImageHub.Models;

namespace ImageHub.Domain.Repositories;

/// <summary>
/// 发布目标储存库
/// </summary>
public interface IPublishTargetRepository : IRepository<PublishTarget, PublishTargetId>
{
    ValueTask<TelegramGroupPublishTarget?> FindByTelegramGroupIdAsync(long groupId, CancellationToken cancellationToken = default);
}