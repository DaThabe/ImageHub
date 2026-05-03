using ImageHub.Enums;

namespace ImageHub.Infrastructure.Services.Sources;

/// <summary>
/// 来源并发配置
/// </summary>
public record SourceConcurrencyOptions
{
    public required int DefaultMaxConcurrency { get; init; }
    public required Dictionary<SourceType, int> Concurrency { get; init; }
}