namespace ImageHub.Events;

/// <summary>
/// 事件处理器
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IDomainEventHandler<T> where T : IDomainEvent
{
    Task HandleAsync(T @event, CancellationToken cancellationToken = default);
}