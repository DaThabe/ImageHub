namespace ImageHub.Infrastructure.Services.Resources;

public record ResourceOptions
{
    /// <summary>
    /// 缓存目录
    /// </summary>
    public required string CacheFolder { get; init; }
}
