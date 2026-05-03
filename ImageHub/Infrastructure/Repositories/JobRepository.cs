using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


internal sealed class JobRepository(ImageHubDbContext dbContext) : RepositoryBase<Job, JobId>(dbContext), IJobRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<Job>> FindActivitysAsync(CancellationToken cancellationToken = default)
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
}
