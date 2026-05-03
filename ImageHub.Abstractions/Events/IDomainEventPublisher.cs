using ImageHub.Events;

namespace ImageHub.Domain.Events;

/// <summary>
/// 领域事件发布器
/// </summary>
public interface IDomainEventPublisher
{
    /// <summary>
    /// 发布事件
    /// </summary>
    /// <param name="event"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task PublsihAsync(IDomainEvent @event, CancellationToken cancellationToken = default);
}