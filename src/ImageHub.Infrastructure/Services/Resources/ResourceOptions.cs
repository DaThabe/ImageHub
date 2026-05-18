namespace ImageHub.Infrastructure.Services.Resources;

public sealed record ResourceOptions
{
    /// <summary>
    /// 缓存目录
    /// </summary>
    public required string CacheFolder { get; init; }
}
