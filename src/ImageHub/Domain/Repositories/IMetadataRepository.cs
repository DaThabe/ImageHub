using ImageHub.Domain.Entities;
using ImageHub.Models;
using ThabeSoft.DomainDrivenDesign;

namespace ImageHub.Domain.Repositories;


/// <summary>
/// 元数据储存库
/// </summary>
public interface IMetadataRepository : IRepository<Metadata, MetadataId>
{
    /// <summary>
    /// 根据来源Id查询
    /// </summary>
    Task<Metadata?> FindBySourceIdAsync(SourceId sourceId, CancellationToken cancellationToken = default);
}