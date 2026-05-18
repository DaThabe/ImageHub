using ImageHub.Domain.Entities;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 任务储存库
/// </summary>
public static class JobRepositoryExtensions
{
    extension(IRepository<Job, JobId> repository)
    {
        public Task<Job?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
        {
            return repository.Query
                .FirstOrDefaultAsync(x => x.SourceId.Equals(sourceId), cancellationToken: cancellationToken);
        }

        public async Task<IReadOnlyCollection<Job>> GetActivitysAsync(CancellationToken cancellationToken = default)
        {
            return await repository.Query
                .Where(x => x.State != Enums.JobState.Failed && x.State != Enums.JobState.Completed)
                .ToListAsync(cancellationToken: cancellationToken);
        }
    }
}