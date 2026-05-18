using ImageHub.Domain.Entities;
using ImageHub.Enums;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 发布任务储存库
/// </summary>
public static class PublishJobRepositoryExtensions
{
    extension(IRepository<PublishJob, PublishJobId> repository)
    {
        public async ValueTask<IReadOnlyCollection<PublishJob>> FindByIdsAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default)
        {
            var id_list = ids.Distinct().ToArray();
            if (id_list.Length == 0) return [];

            return await repository.Query
                 .Where(x => id_list.Contains(x.Id))
                 .ToArrayAsync(cancellationToken);
        }

        public Task<PublishJob?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
        {
            return repository.Query
                .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
        }

        public Task<PublishJob?> FindByTargetIdAndResourceId(PublishTargetId publishTargetId, ResourceId resourceId, CancellationToken cancellationToken = default)
        {
            return repository.Query
               .FirstOrDefaultAsync(x => x.PublishTargetId == publishTargetId && x.ResourceId == resourceId, cancellationToken);
        }

        public async Task<IReadOnlyList<PublishJob>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
        {
            return await repository.Query
                .Where(x => x.State != PublishJobState.Completed)
                .ToArrayAsync(cancellationToken);
        }
    }
}