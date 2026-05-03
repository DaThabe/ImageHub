namespace ImageHub.Enums;


/// <summary>
/// 任务状态
/// </summary>
public enum JobState : byte
{
    /// <summary>
    /// 等待执行
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 元数据下载中
    /// </summary>
    MetadataDownloading = 10,

    /// <summary>
    /// 元数据下载完成
    /// </summary>
    MetadataDownloaded = 11,

    /// <summary>
    /// 资源下载中
    /// </summary>
    ResourceDownloading = 20,

    /// <summary>
    /// 资源下载完成
    /// </summary>
    ResourceDownloaded = 21,

    /// <summary>
    /// 推送中
    /// </summary>
    Publishing = 30,

    /// <summary>
    /// 推送完成
    /// </summary>
    Published = 31,

    /// <summary>
    /// 任务完成
    /// </summary>
    Completed = 254,

    /// <summary>
    /// 错误
    /// </summary>
    Failed = 255,
}
