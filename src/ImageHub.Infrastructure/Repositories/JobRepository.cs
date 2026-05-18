using ImageHub.Entities;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using ImageHub.Repositories;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 任务
/// </summary>
internal sealed class JobRepository(ImageHubDbContext dbContext) : EfCoreRepositoryBase<ImageHubDbContext, Job, JobId>(dbContext), IJobRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<Job>> GetActivitysAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.State != Enums.JobState.Failed && x.State != Enums.JobState.Completed)
            .ToListAsync(cancellationToken: cancellationToken);
    }

    public async Task<Job?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
    }

    public async Task<Job> GetOrCreateBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        // 查询
        var exists = await _dbContext.Jobs
            .AsNoTracking()
            .Where(x => x.SourceId == sourceId)
            .FirstOrDefaultAsync(cancellationToken);
        if (exists is not null) return exists;

        // 创建
        var created = new Job(JobId.Create(), sourceId);
        await _dbContext.AddAsync(created, cancellationToken);

        return created;
    }
}