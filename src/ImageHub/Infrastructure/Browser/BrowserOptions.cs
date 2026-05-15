using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Browser;

/// <summary>
/// 浏览器配置
/// </summary>
public class BrowserOptions : BrowserTypeLaunchOptions
{
    /// <summary>
    /// Cookies
    /// </summary>
    [ConfigurationKeyName("Cookies")]
    public IEnumerable<Cookie> Cookies { get; set; } = [];
}