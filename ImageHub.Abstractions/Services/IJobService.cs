using ImageHub.Models;

namespace ImageHub.Services;


/// <summary>
/// 任务
/// </summary>
public interface IJobService
{
    /// <summary>
    /// 创建任务
    /// </summary>
    /// <param name="url"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<JobId> CreateAsync(string url, CancellationToken cancellationToken = default);
}
