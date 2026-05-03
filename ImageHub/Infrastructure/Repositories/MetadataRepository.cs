using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Repositories;

/// <summary>
/// 元数据储存库
/// </summary>
internal sealed class MetadataRepository(
    ImageHubDbContext dbContext,
    ILogger<MetadataRepository> logger
    ) : RepositoryBase<Metadata, MetadataId>(dbContext), IMetadataRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<Metadata?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        var metadata = await _dbContext.Metadatas
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
        
        if (metadata is not null) return metadata;

        logger.LogTrace("未查询到元数据。SourceId: {SourceId}", sourceId);
        return null;
    }
}