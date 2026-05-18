using ImageHub.Domain.Entities;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 元数据储存库
/// </summary>
public static class MetadataRepositoryExtensions
{
    extension(IRepository<Metadata, MetadataId> repository)
    {
        public Task<Metadata?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
        {
            return repository.Query
                .FirstOrDefaultAsync(x => x.SourceId.Equals(sourceId), cancellationToken: cancellationToken);
        }
    }
}