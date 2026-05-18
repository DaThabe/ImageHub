using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Browser;

/// <summary>
/// 浏览器配置
/// </summary>
public class BrowserOptions
{
    /// <summary>
    /// 是否显示浏览器界面
    /// </summary>
    [ConfigurationKeyName("Headless")]
    public bool Headless { get; set; } = false;

    /// <summary>
    /// Cookies
    /// </summary>
    [ConfigurationKeyName("Cookies")]
    public IEnumerable<Cookie> Cookies { get; set; } = [];
}