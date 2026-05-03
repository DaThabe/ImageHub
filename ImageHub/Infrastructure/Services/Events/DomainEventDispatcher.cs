using ImageHub.Domain.Events;
using ImageHub.Events;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace ImageHub.Infrastructure.Services.Events;


/// <summary>
/// 领域事件分发器
/// </summary>
/// <param name="serviceProvider"></param>
public sealed class DomainEventDispatcher(IServiceScopeFactory scopeFactory) : IDomainEventPublisher
{
    // 处理器类型缓存
    private readonly ConcurrentDictionary<Type, Type> _handlerTypeCache = new();
    // 处理器缓存
    private delegate Task Handler(object eventHandler, object @event, CancellationToken cancellationToken);
    private static readonly ConcurrentDictionary<Type, Handler> _delegateCache = new();


    public async Task PublsihAsync(IDomainEvent @event, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var services = scope.ServiceProvider;

        // 缓存泛型类型
        var event_type = @event.GetType();
        var handler_type = _handlerTypeCache.GetOrAdd(event_type, t => typeof(IDomainEventHandler<>).MakeGenericType(t));

        // 缓存处理委托
        var func = _delegateCache.GetOrAdd(handler_type, t =>
        {
            var handle_method = t.GetMethod(nameof(IDomainEventHandler<>.HandleAsync));
            ArgumentNullException.ThrowIfNull(handle_method);

            var target_param = Expression.Parameter(typeof(object), "target");
            var event_param = Expression.Parameter(typeof(object), "event");
            var ct_param = Expression.Parameter(typeof(CancellationToken), "ct");

            var converted_target = Expression.Convert(target_param, t);
            var converted_event = Expression.Convert(event_param, event_type);

            var call = Expression.Call(converted_target, handle_method, converted_event, ct_param);
            var lambda = Expression.Lambda<Handler>(call, target_param, event_param, ct_param);

            return lambda.Compile();
        });

        // 执行事件
        var handlers = services.GetServices(handler_type);
        foreach (var handler in handlers)
        {
            if (handler is null) continue;
            await func(handler, @event, cancellationToken);
        }
    }
}