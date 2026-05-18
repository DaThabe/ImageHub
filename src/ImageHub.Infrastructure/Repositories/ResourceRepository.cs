using ImageHub.Entities;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using ImageHub.Repositories;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;

/// <summary>
/// 资源储存库
/// </summary>
internal sealed class ResourceRepository(ImageHubDbContext dbContext) :
    EfCoreRepositoryBase<ImageHubDbContext, Resource, ResourceId>(dbContext), IResourceRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<IReadOnlyCollection<Resource>> FindAllByMetadataIdAsync(MetadataId metadataId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resources
           .AsNoTracking()
           .Where(x => x.MetadataId == metadataId)
           .ToListAsync(cancellationToken);
    }

    public async Task<Resource?> FindByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resources
           .AsNoTracking()
           .FirstOrDefaultAsync(x => x.Url == url, cancellationToken);
    }
}