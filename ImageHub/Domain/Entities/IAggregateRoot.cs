using ImageHub.Domain.Events;
using ImageHub.Events;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 聚合根
/// </summary>
public interface IAggregateRoot<TId> : IDomainEventProvider
{
    /// <summary>
    /// 唯一标识符
    /// </summary>
    TId Id { get; }
}

/// <summary>
/// 聚合根
/// </summary>
/// <typeparam name="TId"></typeparam>
public class AggregateRoot<TId> : Entity<TId>, IAggregateRoot<TId>, IDomainEventClearable
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;


    protected AggregateRoot()
    {
    }

    protected AggregateRoot(TId id) : base(id)
    {

    }

    /// <summary>
    /// 添加领域事件
    /// </summary>
    /// <param name="domainEvent"></param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        if (_domainEvents.Contains(domainEvent)) return;
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// 清空领域事件
    /// </summary>
    void IDomainEventClearable.ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}