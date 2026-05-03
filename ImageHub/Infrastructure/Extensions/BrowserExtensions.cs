using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Extensions;

/// <summary>
/// 浏览器扩展
/// </summary>
internal static class BrowserExtensions
{
    /// <summary>
    /// 定位器扩展
    /// </summary>
    /// <param name="locator"></param>
    extension(ILocator locator)
    {
        public async IAsyncEnumerable<ILocator> AllToAsyncEnumerable()
        {
            var all = await locator.AllAsync();
            foreach (var i in all) yield return i;
        }
    }
}
