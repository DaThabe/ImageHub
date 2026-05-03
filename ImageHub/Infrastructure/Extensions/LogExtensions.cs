//using ImageHub.Enums;
//using Microsoft.Extensions.Logging;

//namespace ImageHub.Extensions;

//internal static partial class LogExtensions
//{
//    [LoggerMessage(Level = LogLevel.Debug, Message = "已为 {SourceType} 创建下载器实例。并发限制: {Concurrency}")]
//    public static partial void LogDownloaderCreated(this ILogger logger, SourceType sourceType, int concurrency);


//    [LoggerMessage(Level = LogLevel.Information, Message = "已释放 {Count} 个下载器实例。")]
//    public static partial void LogDownloaderDisposed(this ILogger logger, int count);


//    [LoggerMessage(Level = LogLevel.Debug, Message = "已使用缓存文件: {FileName}")]
//    public static partial void LogUsedFileCache(this ILogger logger, string fileName);


//    [LoggerMessage(Level = LogLevel.Information, Message = "正在下载远程资源...")]
//    public static partial void LogDownloading(this ILogger logger);

//    [LoggerMessage(Level = LogLevel.Information, Message = "下载成功。耗时: {Elapsed}ms, 路径: {Path}")]
//    public static partial void LogDownloadSuccess(this ILogger logger, TimeSpan elapsed, string path);


//    [LoggerMessage(Level = LogLevel.Debug, Message = "初始化缓存目录: {Folder}")]
//    public static partial void LogCacheFolderInitialization(this ILogger logger, string folder);







//}