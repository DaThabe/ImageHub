namespace ImageHub.Enums;

/// <summary>
/// 推送任务状态
/// </summary>
public enum PublishJobState : byte
{
    /// <summary>
    /// 待推送
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 推送成功
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 推送失败
    /// </summary>
    Failed = 2
}