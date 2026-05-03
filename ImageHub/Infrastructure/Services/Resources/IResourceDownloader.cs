using Flurl.Http;
using ImageHub.Enums;
using ImageHub.Infrastructure.Extensions;
using ImageHub.Infrastructure.Services.Sources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace ImageHub.Infrastructure.Services.Resources;

/// <summary>
/// 资源下载器
/// </summary>
public interface IResourceDownloader : IDisposable
{
    /// <summary>
    /// 下载资源
    /// </summary>
    /// <param name="url"></param>
    /// <param name="sourceType"></param>
    /// <param name="useCache"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<string> DownloadAsync(string url, SourceType sourceType, bool useCache = true, CancellationToken cancellationToken = default);
}

/// <summary>
/// 资源下载器
/// </summary>
internal sealed class ResourceDownloader(
    IOptions<ResourceOptions> options, 
    IServiceProvider services, 
    ISourceSemaphoreSlim sourceSemaphoreSlim,
    ILogger<ResourceDownloader> logger) : IResourceDownloader
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
        int count = _downloaders.Count;
        _downloaders.Clear();
        if (count > 0) logger.LogDebug("已释放 {Count} 个下载器实例", count);
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

        logger.LogDebug("已为 {SourceType} 创建下载器实例", type);

        var downloader_logger = services.GetRequiredService<ILogger<FlurlResourceDownloader>>();
        return new FlurlResourceDownloader(client, options, downloader_logger);
    }
}

/// <summary>
/// Flurl 资源下载器
/// </summary>
/// <param name="client"></param>
/// <param name="maxConcurrency"></param>
/// <param name="options"></param>
/// <param name="logger"></param>
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
            logger.LogDebug("已使用缓存文件:{FileName}", file_name);
            return existingFile;
        }

        // 下载文件
        logger.LogDebug("正在下载远程资源");

        var startTime = Stopwatch.GetTimestamp();
        var path = await client.Request(url).DownloadFileAsync(folder, file_name, cancellationToken: cancellationToken);
        var elapsed = Stopwatch.GetElapsedTime(startTime);

        logger.LogInformation("下载成功, 耗时:{Elapsed}ms, 路径:{Path}", elapsed, path);

        return path;
    }


    private void EnsureCacheFolder(string folder)
    {
        if (Directory.Exists(folder)) return;

        logger.LogDebug("初始化缓存目录:{Folder}", folder);
        Directory.CreateDirectory(folder);
    }

    private static string GetMd5Hash(string input)
    {
        byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}