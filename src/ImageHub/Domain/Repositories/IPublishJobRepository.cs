using ImageHub.Domain.Entities;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 发布任务储存库
/// </summary>
public interface IPublishJobRepository : IRepository<PublishJob, PublishJobId>
{
    ValueTask<PublishJob?> FindByTargetIdAndResourceId(PublishTargetId publishTargetId, ResourceId resourceId, CancellationToken cancellationToken = default);


    Task AddRangeAsync(IEnumerable<PublishJob> jobs, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PublishJob>> GetActiveJobsAsync(CancellationToken cancellationToken = default);
    Task<PublishJob?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default);



    Task<IReadOnlyCollection<PublishJob>> FindByIdsAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default);
}