using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;
using ThabeSoft.Mediator;

namespace ImageHub.Domain.Events;


/// <summary>
/// 任务已创建
/// </summary>
/// <param name="JobId">任务Id</param>
/// <param name="SourceId">来源Id</param>
public sealed record JobCreatedDomainEvent(JobId JobId, SourceId SourceId) : INotification, IDomainEvent;