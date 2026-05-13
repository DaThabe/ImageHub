using ImageHub.Domain.Entities;
using ImageHub.Enums;
using ImageHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Services.Metadatas;

/// <summary>
/// Pixiv作品 元数据提取器
/// </summary>
/// <param name="logger"></param>
internal sealed class PixivMetadataExtractor(ILogger<PixivMetadataExtractor> logger) : IMetadataExtractor
{
    public SourceType SupportType { get; } = SourceType.Pixiv;

    public async Task<Metadata> GetAsync(IPage page, Source source, CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Url"] = source.Url,
            ["SourceId"] = source.Id,
            ["SourceType"] = source.Type
        });

        if (logger.IsEnabled(LogLevel.Debug))
        {
            logger.LogDebug("正在进入 Pixiv 页面, Url:{url}", page.Url);
        }

        // 1. 等待核心元素加载
        await page.GotoAsync(source.Url, new PageGotoOptions() { Timeout = 60000 });
        await page.WaitForSelectorAsync("main figure img");

        // 2. 处理“查看全部”折叠逻辑
        var btn = page.Locator("button:has-text('查看全部'), button:has-text('展开')");
        if (await btn.CountAsync() > 0)
        {
            await btn.ClickAsync();
            logger.LogDebug("点击展开图像");

            await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
            await Task.Delay(500, cancellationToken);
        }

        logger.LogDebug("正在获取元数据");
        var snapshot = await GetSnapshotAsync(page);
        logger.LogDebug("元数据获取完成");

        // 4. C# 端进行数据清洗和验证
        if (snapshot.ImgUrls.Length == 0) throw new InvalidOperationException("未获取到图像网址");

        var img_urls = snapshot.ImgUrls.Distinct().ToHashSet();
        var author_url = string.IsNullOrWhiteSpace(snapshot.AuthorUrl)
            ? null
            : $"https://www.pixiv.net{snapshot.AuthorUrl}";

        _ = DateTimeOffset.TryParse(snapshot.DateTimeStr, out var uploadAt);

        return new Metadata(MetadataId.Create(), source.Id, img_urls)
            .ChangeTitle(snapshot.Title)
            .ChangeDescription(snapshot.Describe)
            .ChangeAuthor(snapshot.AuthorName, author_url)
            .ChangeUploadTime(uploadAt)
            .AddTags(snapshot.Tags);
    }

    public static Task<Snapshot> GetSnapshotAsync( IPage page)
    {
        return page.EvaluateAsync<Snapshot>("""
            () => {
                const getVal = (sel) => {
                    const el = document.querySelector(sel);
                    return el ? el.innerText.trim() : "";
                };
        
                return {
                    ImgUrls: Array.from(document.querySelectorAll('figure img')).map(img => img.src),
                    Title: getVal('figcaption h1'),
                    Describe: getVal('figcaption p'),
                    Tags: Array.from(document.querySelectorAll('figcaption footer li a')).map(a => a.innerText.trim()),
                    DateTimeStr: document.querySelector('figcaption time')?.getAttribute('datetime') || '',
                    AuthorName: getVal('aside h2 div > div a'),
                    AuthorUrl: document.querySelector('aside h2 div > div a')?.getAttribute('href') || ''
                };
            }
            """);
    }

    public class Snapshot
    {
        public string[] ImgUrls { get; set; } = [];
        public string Title { get; set; } = "";
        public string Describe { get; set; } = "";
        public string[] Tags { get; set; } = [];
        public string DateTimeStr { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string AuthorUrl { get; set; } = "";
    }
}