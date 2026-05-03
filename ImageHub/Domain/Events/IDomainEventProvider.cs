using ImageHub.Events;

namespace ImageHub.Domain.Events;


/// <summary>
/// 领域事件提供者
/// </summary>
public interface IDomainEventProvider
{
    /// <summary>
    /// 所有领域事件
    /// </summary>
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
}
