using ImageHub.Application.Services;
using ImageHub.Domain.Entities;
using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Services.Sources;
using Microsoft.Extensions.Logging;

namespace ImageHub.Infrastructure.Services.Metadatas;

/// <summary>
/// 元数据下载器
/// </summary>
internal sealed class MetadataProvider(
    IBrowserService browserService,
    ISourceSemaphoreSlim sourceSemaphoreSlim,
    IEnumerable<IMetadataExtractor> extractors,
    ILogger<MetadataProvider> logger
    ) : IMetadataProvider
{
    public async Task<Metadata> FetchAsync(Source source, CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Url"] = source.Url,
            ["SourceId"] = source.Id,
            ["SourceType"] = source.Type
        });

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