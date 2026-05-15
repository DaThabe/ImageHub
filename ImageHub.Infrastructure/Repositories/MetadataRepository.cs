using ImageHub.Entities;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using ImageHub.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 元数据储存库
/// </summary>
internal sealed class MetadataRepository(ImageHubDbContext dbContext) :
    EfCoreRepositoryBase<ImageHubDbContext, Metadata, MetadataId>(dbContext), IMetadataRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<Metadata?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
    }
}