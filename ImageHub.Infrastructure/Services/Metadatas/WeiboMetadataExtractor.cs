using ImageHub.Entities;
using ImageHub.Enums;
using ImageHub.Extensions;
using ImageHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Services.Metadatas;

/// <summary>
/// 微博博客 元数据提取器
/// </summary>
/// <param name="logger"></param>
internal sealed class WeiboMetadataExtractor(ILogger<WeiboMetadataExtractor> logger) : IMetadataExtractor
{
    public SourceType SupportType { get; } = SourceType.Weibo;
    public async Task<Metadata> GetAsync(IPage page, Source source, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("正在进入 微博 页面");

        // 等待页面加载
        await page.GotoAsync(source.Url);
        await page.WaitForSelectorAsync("article.woo-panel-main");

        // 判断是否是转发的微博
        var retweetLocator = page.Locator("article.woo-panel-main div[class*='retweet'] a[href*='/status/']").First;
        if (await retweetLocator.IsVisibleAsync())
        {
            var retweetLink = await retweetLocator.GetAttributeAsync("href");
            if (!string.IsNullOrWhiteSpace(retweetLink))
            {
                var targetUrl = retweetLink.StartsWith("http") ? retweetLink : $"https://weibo.com{retweetLink}";

                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("检测到转发微博, 正在进入原文页面, {targetUrl}", targetUrl);
                }
                await page.WaitForSelectorAsync("article.woo-panel-main");
            }
        }

        logger.LogDebug("正在获取元数据");
        var snapshot = await GetSnapshotAsync(page);
        logger.LogDebug("元数据获取完成");

        // 解析高清图像网址
        var highResUrls = snapshot.ImgUrls
            .Select(url => url.Replace("/orj360/", "/large/")
                             .Replace("/thumb150/", "/large/")
                             .Replace("/mw690/", "/large/"))
            .Distinct()
            .ToHashSet();

        if (highResUrls.Count < 1) throw new InvalidOperationException("未获取到微博图像网址");

        // 处理标签
        var rawText = snapshot.FullText;
        var tags = rawText?.ParseHashSignTag(out rawText).ToHashSet() ?? [];

        // 解析时间 (微博的时间格式多样，可能需要更强的解析逻辑)
        _ = DateTimeOffset.TryParse(snapshot.DateTimeStr, out var uploadAt);

        var authorUrl = string.IsNullOrWhiteSpace(snapshot.AuthorPath)
            ? null
            : (snapshot.AuthorPath.StartsWith("http") ? snapshot.AuthorPath : $"https://weibo.com{snapshot.AuthorPath}");

        return new Metadata(MetadataId.Create(), source.Id, highResUrls)
            .ChangeDescription(rawText)
            .ChangeAuthor(snapshot.AuthorName, authorUrl)
            .ChangeUploadTime(uploadAt)
            .AddTags(tags);
    }

    private static Task<Snapshot> GetSnapshotAsync(IPage page)
    {
        return page.EvaluateAsync<Snapshot>("""
            () => {
                const article = document.querySelector("article.woo-panel-main");
                if (!article) return null;

                const getVal = (root, sel) => root.querySelector(sel)?.innerText?.trim() || "";
                
                // 微博的图片可能在普通九宫格，也可能在查看大图的容器里
                const imgs = Array.from(article.querySelectorAll(".woo-picture-main img, .woo-picture-slot img"))
                                  .map(img => img.src);

                // 微博类名经常变，尝试通过更加通用的选择器获取文本
                const textNode = article.querySelector("div[class*='wbtext']");
                
                // 作者信息通常在头部的链接中
                const authorNode = article.querySelector("a[class*='name']");

                return {
                    ImgUrls: imgs,
                    FullText: textNode?.innerText?.trim() || "",
                    DateTimeStr: article.querySelector("a[class*='time']")?.title || 
                                 article.querySelector("a[class*='time']")?.innerText || "",
                    AuthorName: authorNode?.innerText?.trim() || "",
                    AuthorPath: authorNode?.getAttribute("href") || ""
                };
            }
            """);
    }

    public class Snapshot
    {
        public string[] ImgUrls { get; set; } = [];
        public string FullText { get; set; } = "";
        public string DateTimeStr { get; set; } = "";
        public string AuthorName { get; set; } = "";
        public string AuthorPath { get; set; } = "";
    }
}