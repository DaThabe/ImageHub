using ImageHub.Application.Services;
using ImageHub.Domain.Events;
using ThabeSoft.Mediator;

namespace ImageHub.Application.Handlers;

/// <summary>
/// 任务已创建事件处理
/// </summary>
internal sealed class JobCreatedDomainEventHandler(IJobQueue jobQueue) : INotificationHandler<JobCreatedDomainEvent>
{
    public ValueTask HandleAsync(JobCreatedDomainEvent notification, CancellationToken cancellationToken = default)
    {
        return jobQueue.EnqueueAsync(notification.JobId, cancellationToken);
    }
}