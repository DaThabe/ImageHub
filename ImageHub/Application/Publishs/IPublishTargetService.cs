using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Models;

namespace ImageHub.Application.Publishs;


/// <summary>
/// 发布目标注册器
/// </summary>
public interface IPublishTargetService
{
    /// <summary>
    /// 注册tg群组
    /// </summary>
    /// <param name="groupId"></param>
    /// <returns></returns>
    ValueTask SetTelegramGroup(long groupId, CancellationToken cancellationToken = default);
}

public class PublishTargetService(
    IPublishTargetRepository publishTargetRepository,
    IUnitOfWork unitOfWork
    ) : IPublishTargetService
{
    public async ValueTask SetTelegramGroup(long groupId, CancellationToken cancellationToken = default)
    {
        var target = await publishTargetRepository.FindByTelegramGroupIdAsync(groupId, cancellationToken);
        if (target is not null) return;

        target = new TelegramGroupPublishTarget(PublishTargetId.Create(), groupId);
        await publishTargetRepository.AddAsync(target, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}