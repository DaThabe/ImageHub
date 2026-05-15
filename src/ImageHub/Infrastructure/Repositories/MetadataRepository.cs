using ImageHub.Domain.Entities;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Database;
using ImageHub.Models;
using Microsoft.EntityFrameworkCore;
using ThabeSoft.DomainDrivenDesign.EntityFrameworkCore;

namespace ImageHub.Infrastructure.Repositories;


/// <summary>
/// 元数据储存库
/// </summary>
internal sealed class MetadataRepository(ImageHubDbContext dbContext) :
    RepositoryBase<ImageHubDbContext, Metadata, MetadataId>(dbContext), IMetadataRepository
{
    private readonly ImageHubDbContext _dbContext = dbContext;

    public async Task<Metadata?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Metadatas
            .FirstOrDefaultAsync(x => x.SourceId == sourceId, cancellationToken);
    }
}