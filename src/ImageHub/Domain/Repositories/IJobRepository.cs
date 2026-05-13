using ImageHub.Domain.Entities;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 任务
/// </summary>
public interface IJobRepository : IRepository<Job, JobId>
{
    Task<Job?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default);
    Task<Job> GetOrCreateBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Job>> GetActivitysAsync(CancellationToken cancellationToken = default);
}