using ImageHub.Models;

namespace ImageHub.Events;


/// <summary>
/// 任务已创建
/// </summary>
/// <param name="JobId">任务Id</param>
/// <param name="SourceId">来源Id</param>
public record JobCreatedDomainEvent(JobId JobId, SourceId SourceId) : IDomainEvent;