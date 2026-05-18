using ImageHub.Domain.Entities;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 资源储存库
/// </summary>
public static class ResourceRepositoryExtensions
{
    extension(IRepository<Resource, ResourceId> repository)
    {
        public async Task<IReadOnlyCollection<Resource>> FindAllByMetadataIdAsync(MetadataId metadataId, CancellationToken cancellationToken = default)
        {
            return await repository.Query
               .Where(x => x.MetadataId == metadataId)
               .ToArrayAsync(cancellationToken);
        }

        public async ValueTask<Resource?> FindByUrlAsync(string url, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(url)) return default;

            return await repository.Query
               .FirstOrDefaultAsync(x => x.Url == url, cancellationToken);
        }
    }
}