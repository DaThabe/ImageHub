using ImageHub.Domain.Entities;
using ImageHub.Models;

namespace ImageHub.Domain.Repositories;

/// <summary>
/// 资源储存库
/// </summary>
public interface IResourceRepository : IRepository<Resource, ResourceId>
{
    Task<Resource?> FindByUrlAsync(string url, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Resource>> FindAllByMetadataIdAsync(MetadataId metadataId, CancellationToken cancellationToken = default);
}