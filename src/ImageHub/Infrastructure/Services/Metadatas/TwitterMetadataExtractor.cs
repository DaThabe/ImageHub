using Flurl;
using ImageHub.Domain.Entities;
using ImageHub.Enums;
using ImageHub.Extensions;
using ImageHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Services.Metadatas;

/// <summary>
/// 推特推文 元数据提取器
/// </summary>
/// <param name="logger"></param>
internal sealed class TwitterMetadataExtractor(ILogger<TwitterMetadataExtractor> logger) : IMetadataExtractor
{
    public SourceType SupportType { get; } = SourceType.Twitter;
    public async Task<Metadata> GetAsync(IPage page, Source source, CancellationToken cancellationToken = default)
    {
        using var scope = logger.BeginScope(new Dictionary<string, object>
        {
            ["Url"] = source.Url,
            ["SourceId"] = source.Id,
            ["SourceType"] = source.Type
        });

        logger.LogDebug("正在进入 Twitter 页面");

        // 等待页面加载
        await page.GotoAsync(source.Url);
        await page.WaitForSelectorAsync("article[data-testid='tweet']", new() { State = WaitForSelectorState.Visible });

        logger.LogDebug("正在获取元数据");
        var snapshot = await GetSnapshotAsync(page);
        logger.LogDebug("元数据获取完成");

        // Twitter 图片 src 示例: https://pbs.twimg.com/media/xxx?format=jpg&name=large
        var highResUrls = snapshot.ImgUrls
            .Select(url =>
            {
                if (string.IsNullOrWhiteSpace(url)) return null;
                try
                {
                    var uri = new Uri(url);
                    // 强制将 name 参数改为 orig 或 4096x4096 以获取原图
                    return url.SetQueryParam("name", "orig").ToString();
                }
                catch { return null; }
            })
            .Where(x => x != null)
            .Cast<string>()
            .Distinct()
            .ToHashSet();

        if (highResUrls.Count < 1) throw new InvalidOperationException("未获取到推文图像网址");

        // 4. 处理标签 (利用你已有的扩展方法 ParseHashSignTag)
        var rawDescribe = snapshot.FullText;
        var tags = rawDescribe?.ParseHashSignTag(out rawDescribe).ToHashSet() ?? [];

        // 5. 解析时间
        _ = DateTimeOffset.TryParse(snapshot.DateTimeStr, out var uploadAt);

        var author_url = string.IsNullOrWhiteSpace(snapshot.AuthorPath)
            ? null
            : $"https://x.com{snapshot.AuthorPath}";

        return new Metadata(MetadataId.Create(), source.Id, highResUrls)
            .ChangeDescription(rawDescribe)
            .ChangeAuthor(snapshot.AuthorName, author_url)
            .ChangeUploadTime(uploadAt)
            .AddTags(tags);
    }

    private static Task<Snapshot> GetSnapshotAsync(IPage page)
    {
        return page.EvaluateAsync<Snapshot>("""
            () => {
                // 定位主推文容器 (防止拿到评论里的图片)
                const tweet = document.querySelector("article[data-testid='tweet']");
                if (!tweet) return null;

                const getVal = (root, sel) => root.querySelector(sel)?.innerText?.trim() || "";
                
                return {
                    // 只获取该推文内的图片，过滤掉头像
                    ImgUrls: Array.from(tweet.querySelectorAll("div[data-testid='tweetPhoto'] img")).map(img => img.src),
                    FullText: getVal(tweet, "[data-testid='tweetText']"),
                    DateTimeStr: tweet.querySelector("time")?.getAttribute("datetime") || "",
                    AuthorName: getVal(tweet, "[data-testid='User-Name'] a"),
                    AuthorPath: tweet.querySelector("[data-testid='User-Name'] a")?.getAttribute("href") || ""
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
