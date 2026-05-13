namespace ImageHub.Application.Services;


/// <summary>
/// 发布目标注册器
/// </summary>
public interface IPublishTargetService
{
    /// <summary>
    /// 注册tg群组
    /// </summary>
    ValueTask SetTelegramGroup(long groupId, CancellationToken cancellationToken = default);
}