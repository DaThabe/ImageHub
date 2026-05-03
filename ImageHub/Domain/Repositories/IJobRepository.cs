using ImageHub.Domain.Entities;
using ImageHub.Models;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 任务
/// </summary>
public interface IJobRepository : IRepository<Job, JobId>
{
    Task<Job?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<Job>> FindActivitysAsync(CancellationToken cancellationToken = default);
}
