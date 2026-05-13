using Flurl.Http;
using ImageHub.Enums;
using ImageHub.Infrastructure.Services.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Telegram.Bot.Types;

namespace ImageHub.Infrastructure.Services.Resources;

/// <summary>
/// 资源下载器
/// </summary>
internal interface IResourceDownloader : IDisposable
{
    /// <summary>
    /// 下载资源
    /// </summary>
    Task<string> DownloadAsync(string url, SourceType sourceType, bool useCache = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// 资源下载器
/// </summary>
internal sealed class ResourceDownloader(
    IOptions<ResourceOptions> options,
    IServiceProvider services,
    ISourceSemaphoreSlim sourceSemaphoreSlim
    ) : IResourceDownloader
{
    private readonly ConcurrentDictionary<SourceType, FlurlResourceDownloader> _downloaders = [];

    public async Task<string> DownloadAsync(string url, SourceType sourceType, bool useCache = true, CancellationToken cancellationToken = default)
    {
        await sourceSemaphoreSlim.WaitAsync(sourceType, cancellationToken);
        try
        {
            var downloader = _downloaders.GetOrAdd(sourceType, _ => CreateDownloader(sourceType));
            return await downloader.DownloadAsync(url, cancellationToken);
        }
        finally
        {
            sourceSemaphoreSlim.Release(sourceType);
        }
    }

    public void Dispose()
    {
        _downloaders.Clear();
    }





    private FlurlResourceDownloader CreateDownloader(SourceType type)
    {
        // 默认配置
        var client = new FlurlClient();

        switch (type)
        {
            case SourceType.Pixiv:
                // Pixiv 必须校验 Referer
                client.WithHeader("Referer", "https://www.pixiv.net/");
                break;
            case SourceType.Twitter:
                break;
        }

        var downloader_logger = services.GetRequiredService<ILogger<FlurlResourceDownloader>>();
        return new FlurlResourceDownloader(client, options, downloader_logger);
    }
}

/// <summary>
/// Flurl 资源下载器
/// </summary>
internal sealed class FlurlResourceDownloader(IFlurlClient client, IOptions<ResourceOptions> options, ILogger<FlurlResourceDownloader> logger)
{
    public bool UseCache { get; set; } = true;


    public async Task<string> DownloadAsync(string url, CancellationToken cancellationToken = default)
    {
        // 开启日志上下文
        using var _ = logger.BeginScope(new Dictionary<string, object> { ["Url"] = url });


        // 缓存检查
        string file_name = GetMd5Hash(url);
        var folder = options.Value.CacheFolder;

        // 确保缓存目录存在
        EnsureCacheFolder(folder);

        var existingFile = Directory.EnumerateFiles(folder, $"{file_name}.*").FirstOrDefault();
        if (existingFile != null && UseCache)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("已使用缓存文件: {fileName}", file_name);
            }
            return existingFile;
        }

        // 下载文件
        logger.LogDebug("正在下载远程资源");

        var startTime = Stopwatch.GetTimestamp();
        var path = await client.Request(url).DownloadFileAsync(folder, file_name, cancellationToken: cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startTime);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("下载完成, 耗时:{elapsedilliseconds}, 路径:{path}", elapsed, path);
        }

        return path;
    }


    private static void EnsureCacheFolder(string folder)
    {
        if (Directory.Exists(folder)) return;
        Directory.CreateDirectory(folder);
    }

    private static string GetMd5Hash(string input)
    {
        byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}