using ImageHub.Domain.Entities;
using ImageHub.Enums;
using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Services.Sources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace ImageHub.Application.Metadatas;


/// <summary>
/// 元数据下载策略
/// </summary>
public interface IMetadataOrchestrator
{
    Task<Metadata> FetchAsync(Source source, CancellationToken cancellationToken = default);
}

/// <summary>
/// 元数据下载器
/// </summary>
/// <param name="browserService"></param>
/// <param name="extractors"></param>
/// <param name="logger"></param>
internal sealed class MetadataOrchestrator(
    IBrowserService browserService,
    ISourceSemaphoreSlim sourceSemaphoreSlim,
    IEnumerable<IMetadataExtractor> extractors,
    ILogger<MetadataOrchestrator> logger
    ) : IMetadataOrchestrator
{
    public async Task<Metadata> FetchAsync(Source source, CancellationToken cancellationToken = default)
    {
        await sourceSemaphoreSlim.WaitAsync(source.Type, cancellationToken);
        var page = await browserService.SharedContext.NewPageAsync();

        try
        {
            var extractor = extractors.FirstOrDefault(x => x.SupportType == source.Type);
            if (extractor is null)
            {
                logger.LogError("未找到匹配的提取器。Type: {Type}", source.Type);
                throw new NotSupportedException($"Unsupported source type: {source.Type}");
            }

            return await extractor.GetAsync(page, source, cancellationToken);
        }
        finally
        {
            sourceSemaphoreSlim.Release(source.Type);

            await page.CloseAsync();
            logger.LogDebug("浏览器页面已关闭。");
        }
    }
}