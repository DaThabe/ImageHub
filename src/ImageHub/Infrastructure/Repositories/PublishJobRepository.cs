using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Enums;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 发布任务储存库
/// </summary>
internal sealed class PublishJobRepository(ImageHubDbContext dbContext) :
   RepositoryBase<ImageHubDbContext, PublishJob, PublishJobId>(dbContext), IPublishJobRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task AddRangeAsync(IEnumerable<PublishJob> jobs, CancellationToken cancellationToken = default)
    {
        await _dbContext
            .AddRangeAsync(jobs, cancellationToken);
    }

    public async Task<IReadOnlyCollection<PublishJob>> FindByIdsAsync(IEnumerable<PublishJobId> ids, CancellationToken cancellationToken = default)
    {
        var id_list = ids.Distinct().ToArray();
        if (id_list.Length == 0) return [];

        return await _dbContext.PublishJobs
            .Where(x => id_list.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<PublishJob?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PublishJobs
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
    }

    public async ValueTask<PublishJob?> FindByTargetIdAndResourceId(PublishTargetId publishTargetId, ResourceId resourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PublishJobs
           .FirstOrDefaultAsync(x => x.PublishTargetId == publishTargetId && x.ResourceId == resourceId, cancellationToken);
    }

    public async Task<IReadOnlyList<PublishJob>> GetActiveJobsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _dbContext.PublishJobs
            .Where(x => x.State != PublishJobState.Completed)
            .ToListAsync(cancellationToken);

        return items.AsReadOnly();
    }
}
