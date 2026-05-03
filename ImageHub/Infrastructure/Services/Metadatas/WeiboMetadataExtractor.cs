using ImageHub.Application.Metadatas;
using ImageHub.Domain.Entities;
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
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Url"] = source.Url,
            ["SourceId"] = source.Id,
            ["SourceType"] = source.Type
        });

        logger.LogDebug("正在进入 微博 页面");

        // 等待页面加载
        await page.GotoAsync(source.Url);
        await page.WaitForSelectorAsync("article.woo-panel-main");

        // 2. 使用一个不带“自动等待”的方法来检查是否存在转发链接
        // 这里的关键是：不要直接 GetAttribute，先看看它在不在
        var retweetLocator = page.Locator("article.woo-panel-main div[class*='retweet'] a[href*='/status/']").First;

        // 检查元素是否在 2 秒内出现，而不是死等 30 秒
        if (await retweetLocator.IsVisibleAsync())
        {
            var retweetLink = await retweetLocator.GetAttributeAsync("href");
            if (!string.IsNullOrWhiteSpace(retweetLink))
            {
                var targetUrl = retweetLink.StartsWith("http") ? retweetLink : $"https://weibo.com{retweetLink}";
                logger.LogDebug("检测到转发微博, 正在进入原文页面:{Url}", targetUrl);
                await page.GotoAsync(targetUrl);
                await page.WaitForSelectorAsync("article.woo-panel-main");
            }
        }

        logger.LogDebug("正在获取元数据");
        var snapshot = await GetSnapshotAsync(page);
        logger.LogDebug("元数据获取完成");

        // 3. 数据清洗 (微博图片最高清化)
        // 微博 src 通常是: .../orj360/... 或 .../mw2000/...
        // 我们统一替换为 /large/ 或 /oslarge/ 以获取原图
        var highResUrls = snapshot.ImgUrls
            .Select(url => url.Replace("/orj360/", "/large/")
                             .Replace("/thumb150/", "/large/")
                             .Replace("/mw690/", "/large/"))
            .Distinct()
            .ToHashSet();

        if (highResUrls.Count < 1) throw new InvalidOperationException("未获取到微博图像网址");

        // 4. 处理标签
        var rawText = snapshot.FullText;
        var tags = rawText?.ParseHashSignTag(out rawText).ToHashSet() ?? [];

        // 5. 解析时间 (微博的时间格式多样，可能需要更强的解析逻辑)
        _ = DateTimeOffset.TryParse(snapshot.DateTimeStr, out var uploadAt);

        var authorUrl = string.IsNullOrWhiteSpace(snapshot.AuthorPath)
            ? null
            : (snapshot.AuthorPath.StartsWith("http") ? snapshot.AuthorPath : $"https://weibo.com{snapshot.AuthorPath}");

        return new Metadata(MetadataId.Create(), source.Id, highResUrls)
            .ChangeDescription(rawText)
            .ChangeAuthor(snapshot.AuthorName, authorUrl)
            .ChangeUploadTime(uploadAt)
            .AddTags(tags);

        //string url = source.Url;

        //await page.GotoAsync(url);
        //logger.LogTrace("正在获取 [微博] 图片信息: {url}", page.Url);

        ////等待微博内容加载完成
        //await page.WaitForSelectorAsync("article.woo-panel-main");

        //// 是转发的微博
        //if (await page.Locator("article.woo-panel-main .retweet").CountAsync() > 0)
        //{
        //    var target = page.Locator("article.woo-panel-main .retweet > .woo-box-flex a");
        //    var target_url = await target.GetAttributeAsync("href");

        //    url = target_url;
        //    await page.GotoAsync(url!);
        //    await page.WaitForSelectorAsync("article.woo-panel-main");
        //}

        ////获取图片链接
        //var img_urls = await page.Locator("article.woo-panel-main .woo-picture-main img").AllToAsyncEnumerable()
        //    .Select(async (ILocator x, CancellationToken _) => await x.GetAttributeAsync("src"))
        //    .Where(x => !string.IsNullOrWhiteSpace(x))
        //    .Select(x => x!)
        //    .Distinct()
        //    .ToHashSetAsync(cancellationToken: cancellationToken);
        //if (img_urls.Count < 1) throw new InvalidOperationException("未获取到图像网址");

        ////标题
        ////var title = await page.FindElement("#detail-title").InnerTextAsync();
        ////描述
        //var describe = await page.Locator("article.woo-panel-main ._wbtext_1psp9_14").InnerTextAsync();
        ////标签
        //var tags = describe?.ParseHashSignTag(out describe).ToHashSet() ?? [];
        ////发布时间
        //// _ = DateTimeOffset.TryParse(await tweet_element.Locator("time").GetAttributeOrDefaultAsync("datetime"), out DateTimeOffset time);

        ////作者元素
        ////var author_element = page.FindElement(".interaction-container .author-container .info");
        ////作者名称
        ////var author_name = await author_element.FindElement(".username").InnerTextAsync();
        ////作者Url
        ////var author_url = await page.FindElement(".author-wrapper a").GetAttributeAsync("href");
        ////if (author_url != null) author_url = $"https://www.xiaohongshu.com{author_url}";

        //return new Metadata(MetadataId.Create(), source.Id)
        //{
        //    Resources = img_urls.AsReadOnly(),
        //    Description = describe,
        //    //AuthorName = author_name,
        //    //AuthorUrl = author_url,
        //    Tags = tags.AsReadOnly(),
        //    //UploadAt = time
        //};
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