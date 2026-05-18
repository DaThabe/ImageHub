using ImageHub.Domain.Entities;
using ImageHub.Enums;
using ImageHub.Extensions;
using ImageHub.Infrastructure.Browser;
using ImageHub.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Services.Metadatas;

/// <summary>
/// 小红书笔记 元数据提取器
/// </summary>
/// <param name="logger"></param>
internal sealed class XiaoHongShuMetadataExtractor(ILogger<XiaoHongShuMetadataExtractor> logger) : IMetadataExtractor
{
    public SourceType SupportType { get; } = SourceType.XiaoHongShu;
    public async Task<Metadata> GetAsync(IPage page, Source source, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("正在进入小红书页面");

        // 等待加载
        await page.GotoAsync(source.Url, new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 3000 });
        await page.WaitForSelectorAsync(".swiper-wrapper img");

        logger.LogDebug("正在获取元数据");

        //获取图片链接
        var img_urls = await page.Locator(".swiper-wrapper img").AllToAsyncEnumerable()
            .Select(async (ILocator x, CancellationToken _) => await x.GetAttributeAsync("src"))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct()
            .ToHashSetAsync(cancellationToken: cancellationToken);
        if (img_urls.Count < 1) throw new InvalidOperationException("未获取到图像网址");

        //标题
        var title = await page.Locator("#detail-title").InnerTextAsync();
        //描述
        var describe = await page.Locator("#detail-desc").InnerTextAsync();
        //标签
        var tags = describe?.ParseHashSignTag(out describe).ToHashSet() ?? [];
        //发布时间
        //_ = DateTimeOffset.TryParse(await tweet_element.Locator("time").GetAttributeOrDefaultAsync("datetime"), out DateTimeOffset time);

        //作者元素
        var author_element = page.Locator(".interaction-container .author-container .info");
        //作者名称
        var author_name = await author_element.Locator(".username").InnerTextAsync();
        //作者Url
        var author_url = await author_element.Locator("a.name").GetAttributeAsync("href");
        if (author_url != null) author_url = $"https://www.xiaohongshu.com{author_url}";

        logger.LogDebug("元数据获取完成");

        return new Metadata(MetadataId.Create(), source.Id, img_urls)
            .ChangeDescription(describe)
            .ChangeAuthor(author_name, author_url)
            //.ChangeUploadTime(uploadAt)
            .AddTags(tags);
    }
}
