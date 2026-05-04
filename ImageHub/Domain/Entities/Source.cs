using ImageHub.Enums;
using ImageHub.Models;
using System.Security.Cryptography;
using System.Text;

namespace ImageHub.Domain.Entities;


/// <summary>
/// 来源网址
/// </summary>
public abstract class Source : AggregateRoot<SourceId>
{
    /// <summary>
    /// 类型
    /// </summary>
    public abstract SourceType Type { get; }

    /// <summary>
    /// 网址
    /// </summary>
    public abstract string Url { get; }



    protected Source()
    {
    }

    protected Source(SourceId id) : base(id)
    {

    }


    protected static Guid CreateFromString(string value)
    {
        if (string.IsNullOrEmpty(value)) return Guid.Empty;

        // 1. 将命名空间 Guid 转换为字节数组并处理大端/小端字节序
        byte[] namespaceBytes = Namespace.ToByteArray();
        SwapByteOrder(namespaceBytes);

        // 2. 将输入字符串转换为 UTF8 字节
        byte[] nameBytes = Encoding.UTF8.GetBytes(value);

        // 3. 合并字节并计算 SHA1 哈希
        byte[] hash;
        using (var algorithm = SHA1.Create())
        {
            algorithm.TransformBlock(namespaceBytes, 0, namespaceBytes.Length, null, 0);
            algorithm.TransformFinalBlock(nameBytes, 0, nameBytes.Length);
            hash = algorithm.Hash!;
        }

        // 4. 取哈希的前 16 字节并设置版本号 (v5) 和变体号
        byte[] newGuid = new byte[16];
        Array.Copy(hash, 0, newGuid, 0, 16);

        newGuid[6] = (byte)((newGuid[6] & 0x0F) | (5 << 4)); // Version 5
        newGuid[8] = (byte)((newGuid[8] & 0x3F) | 0x80);    // Variant RFC 4122

        // 5. 再次处理字节序并返回
        SwapByteOrder(newGuid);
        return new Guid(newGuid);
    }
    private static readonly Guid Namespace = new("e1234567-89ab-cdef-0123-456789abcdef");
    private static void SwapByteOrder(byte[] guid)
    {
        Swap(guid, 0, 3);
        Swap(guid, 1, 2);
        Swap(guid, 4, 5);
        Swap(guid, 6, 7);

        static void Swap(byte[] b, int i, int j)
        {
            (b[i], b[j]) = (b[j], b[i]);
        }
    }
}


/// <summary>
/// Pixiv 作品页网址
/// </summary>
public class PixivArtworksSource : Source
{
    public int Pid { get; }
    public override SourceType Type => SourceType.Pixiv;
    public override string Url => $"https://www.pixiv.net/artworks/{Pid}";


    private PixivArtworksSource(SourceId id, int pid) : base(id)
    {
        Pid = pid;
    }
    public static PixivArtworksSource Create(int pid)
    {
        if (pid < 0) throw new ArgumentOutOfRangeException(nameof(pid), "Pixiv 作品 ID 格式不正确，必须为正整数。");

        var id = new SourceId(CreateFromString($"PixivArtworksSource_{pid}"));
        return new PixivArtworksSource(id, pid);
    }

    public override string ToString()
    {
        return Url;
    }
}

/// <summary>
/// 推特推文来源
/// </summary>
public class TwitterTweetSource : Source
{
    public string Username { get; }
    public long TweetId { get; }
    public override SourceType Type => SourceType.Twitter;
    public override string Url => $"https://x.com/{Username}/status/{TweetId}";


    private TwitterTweetSource(SourceId id, string username, long tweetId) : base(id)
    {
        Username = username;
        TweetId = tweetId;
    }
    public static TwitterTweetSource Create(string username, long tweetId)
    {
        //推特用户名大小写不敏感, 所以统一转为小写
        username = username.Trim().ToLower();

        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var id = new SourceId(CreateFromString($"TwitterTweetSource_{username}_{tweetId}"));
        return new TwitterTweetSource(id, username, tweetId);
    }

    public override string ToString()
    {
        return Url;
    }
}

/// <summary>
/// 小红书笔记来源
/// </summary>
public class XiaoHongShuNoteSource : Source
{
    public string NoteId { get; }
    public string XsecToken { get; }
    public override SourceType Type => SourceType.XiaoHongShu;
    public override string Url => $"https://www.xiaohongshu.com/explore/{NoteId}?xsec_token={XsecToken}";


    private XiaoHongShuNoteSource(SourceId id, string noteId, string xsecToken) : base(id)
    {
        NoteId = noteId;
        XsecToken = xsecToken;
    }
    public static XiaoHongShuNoteSource Create(string noteId, string xsecToken)
    {
        noteId = noteId.Trim();
        xsecToken = xsecToken.Trim();

        ArgumentException.ThrowIfNullOrWhiteSpace(noteId);
        ArgumentException.ThrowIfNullOrWhiteSpace(xsecToken);

        var id = new SourceId(CreateFromString($"XiaoHongShuNoteSource_{noteId}_{xsecToken}"));
        return new XiaoHongShuNoteSource(id, noteId, xsecToken);
    }

    public override string ToString()
    {
        return Url;
    }
}

/// <summary>
/// 微博博客来源
/// </summary>
public class WeiboBlogSource : Source
{
    public long UserId { get; }
    public string BlogId { get; }
    public override SourceType Type => SourceType.Weibo;
    public override string Url => $"https://weibo.com/{UserId}/{BlogId}";


    private WeiboBlogSource(SourceId id, long userId, string blogId) : base(id)
    {
        UserId = userId;
        BlogId = blogId;
    }
    public static WeiboBlogSource Create(long userId, string blogId)
    {
        blogId = blogId.Trim();

        if (userId < 0) throw new ArgumentOutOfRangeException(nameof(userId), "微博作者 ID 格式不正确，必须为正整数。");
        ArgumentException.ThrowIfNullOrWhiteSpace(blogId);

        var id = new SourceId(CreateFromString($"WeiboBlogSource_{userId}_{blogId}"));
        return new WeiboBlogSource(id, userId, blogId);
    }

    public override string ToString()
    {
        return Url;
    }
}