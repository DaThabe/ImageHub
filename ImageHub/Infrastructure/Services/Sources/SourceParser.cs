using ImageHub.Domain.Entities;
using ImageHub.Domain.Services;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ImageHub.Sources;

/// <summary>
/// 将网址转为来源
/// </summary>
/// <param name="logger"></param>
internal sealed partial class SourceParser : ISourceParser
{
    public Source Parse(string url)
    {
        if (TryParse(url, out var source)) return source;
        throw new InvalidOperationException("无法解析的Url");
    }

    public bool TryParse(string url, [NotNullWhen(true)] out Source? source)
    {
        source = null;

        // Pixiv
        if (TryParsePixivArtworksUrl(url, out var pid))
        {
            source = PixivArtworksSource.Create(pid);
        }
        // 推特
        else if (TryParseTwiiterTweetUrl(url, out var username, out var tweetId))
        {
            source = TwitterTweetSource.Create(username, tweetId);
        }
        // 小红书
        else if (TryParseXiaohongshuNoteUrl(url, out var noteId, out var token))
        {
            source = XiaoHongShuNoteSource.Create(noteId, token);
        }
        // 微博
        else if (TryParseWeiboBlogUrl(url, out var userId, out var blogId))
        {
            source = WeiboBlogSource.Create(userId, blogId);
            return true;
        }

        // 返回是否获取到来源
        return source is not null;
    }

    /// <summary>
    /// 尝试解析Pid
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool TryParsePixivArtworksUrl(string url, out int pid)
    {
        pid = 0;

        var match_result = PixivArtworksUrlRegex().Match(url);
        if (!match_result.Success) return false;

        return int.TryParse(match_result.Groups["pid"].Value, out pid);
    }
    /// <summary>
    /// 尝试解析用户名和推文Id
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool TryParseTwiiterTweetUrl(string url, out string username, out long tweetId)
    {
        username = string.Empty;
        tweetId = 0;

        var match_result = TwiiterTweetUrlRegex().Match(url);
        if (!match_result.Success) return false;

        username = match_result.Groups["user"].Value;
        return long.TryParse(match_result.Groups["tid"].Value, out tweetId);
    }
    /// <summary>
    /// 尝试解析用户名和推文Id
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool TryParseXiaohongshuNoteUrl(string url, out string noteId, out string token)
    {
        noteId = string.Empty;
        token = string.Empty;

        var match_result = XiaohongshuNoteUrlRegex().Match(url);
        if (!match_result.Success) return false;


        noteId = match_result.Groups["noteId"].Value;
        token = match_result.Groups["token"].Value;

        return true;
    }
    /// <summary>
    /// 尝试解析用户Id和微博Id
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    private static bool TryParseWeiboBlogUrl(string url, out long userId, out string blogId)
    {
        userId = 0;
        blogId = string.Empty;

        var match_result = WeiboBlogUrlRegex().Match(url);
        if (!match_result.Success) return false;

        blogId = match_result.Groups["blogId"].Value;
        return long.TryParse(match_result.Groups["userId"].Value, out userId);
    }



    // Pixiv作品连接匹配 https://www.pixiv.net/artworks/{pid}
    [GeneratedRegex(@"https?:\/\/www\.pixiv\.net\/artworks\/(?<pid>\d+)")]
    private static partial Regex PixivArtworksUrlRegex();

    // 推特推文链接匹配 https://twitter.com/{username}/status/{tweetId} 或 https://x.com/{username}/status/{tweetId}
    [GeneratedRegex(@"https?:\/\/(?:www\.)?(?:x|twitter)\.com\/(?<user>[^\/]+)\/status\/(?<tid>\d+)")]
    private static partial Regex TwiiterTweetUrlRegex();

    // 小红书笔记链接匹配 https://www.xiaohongshu.com/explore/{noteId}?xsec_token={xsec_token}
    [GeneratedRegex(@"https?:\/\/www\.xiaohongshu\.com\/explore\/(?<noteId>[a-zA-Z0-9]+)\?xsec_token=(?<token>[^&\s]+)")]
    private static partial Regex XiaohongshuNoteUrlRegex();

    // 微博博客链接匹配 https://weibo.com/{userId}/{blogId}
    [GeneratedRegex(@"https?:\/\/weibo\.com\/(?<userId>\d+)\/(?<blogId>[a-zA-Z0-9]+)")]
    private static partial Regex WeiboBlogUrlRegex();

    
}