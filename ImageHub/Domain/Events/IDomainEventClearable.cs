namespace ImageHub.Domain.Events;

/// <summary>
/// 可以清除领域事件的
/// </summary>
public interface IDomainEventClearable
{
    void ClearDomainEvents();
}