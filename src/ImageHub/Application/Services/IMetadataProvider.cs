using ImageHub.Domain.Entities;

namespace ImageHub.Application.Services;


/// <summary>
/// 元数据下载策略
/// </summary>
public interface IMetadataProvider
{
    /// <summary>
    /// 根据来源获取元数据
    /// </summary>
    Task<Metadata> FetchAsync(Source source, CancellationToken cancellationToken = default);
}