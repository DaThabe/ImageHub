using ImageHub.Domain.Entities;
using System.Diagnostics.CodeAnalysis;

namespace ImageHub.Domain.Services;


/// <summary>
/// 将网址转为来源
/// </summary>
public interface ISourceParser
{
    /// <summary>
    /// 分析网址并储存来源
    /// </summary>
    /// <param name="url"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    bool TryParse(string url, [NotNullWhen(true)] out Source? source);
}