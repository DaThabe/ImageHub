using ImageHub.Domain.Entities;
using ImageHub.Enums;
using Microsoft.Playwright;

namespace ImageHub.Application.Metadatas;


/// <summary>
/// 元数据提取器
/// </summary>
public interface IMetadataExtractor
{
    /// <summary>
    /// 支持提取的类型
    /// </summary>
    SourceType SupportType { get; }

    /// <summary>
    /// 从来源获取元数据
    /// </summary>
    /// <param name="page"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Metadata> GetAsync(IPage page, Source souce, CancellationToken cancellationToken = default);
}