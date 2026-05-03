using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Playwright;

namespace ImageHub.Infrastructure.Browser;


/// <summary>
/// 浏览器业务
/// </summary>
internal interface IBrowserService : IHostedService
{
    IPlaywright Playwright { get; }
    IBrowser SharedBrowser { get; }
    IBrowserContext SharedContext { get; }
}

internal sealed class BrowserService(IOptions<BrowserOptions> options, ILogger<BrowserService> logger) : IBrowserService
{
    private IPlaywright? _playwright;
    private IBrowser? _sharedBrowser;
    private IBrowserContext? _sharedContext;


    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("浏览器服务未启动");
    public IBrowser SharedBrowser => _sharedBrowser ?? throw new InvalidOperationException("浏览器服务未启动");
    public IBrowserContext SharedContext => _sharedContext ?? throw new InvalidOperationException("浏览器服务未启动");


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogDebug("正在初始化 Playwright 引擎 (Headless: {Headless})...", options.Value.Headless);

            // 初始化 Playwright
            if (_playwright is null)
            {
                logger.LogInformation("检查并安装 Chromium 内核依赖...");
                Program.Main(["install", "chromium"]);

                _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
                logger.LogDebug("Playwright 引擎实例初始化成功。");
            }

            // 初始化 共享浏览器实例
            if (_sharedBrowser is null)
            {
                var launchOption = new BrowserTypeLaunchOptions { Headless = options.Value.Headless };
                _sharedBrowser = await _playwright.Chromium.LaunchAsync(launchOption);
                logger.LogDebug("已经创建共享浏览器实例。");
            }


            // 初始化 共享上下文
            if (_sharedContext is null)
            {
                int cookieCount = options.Value.Cookies?.Count() ?? 0;
                _sharedContext = await _sharedBrowser.NewContextAsync();

                if (cookieCount > 0) await _sharedContext.AddCookiesAsync(options.Value.Cookies!);
                logger.LogDebug("已创建共享上下文, 共注入 {Count} 条 Cookie。", cookieCount);
            }

            logger.LogInformation("浏览器服务初始化完成。");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "浏览器服务初始化失败！引擎无法启动。原因: {Message}", ex.Message);
            throw;
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("正在关闭浏览器服务...");

        try
        {
            if (_sharedContext is not null)
            {
                await _sharedContext.CloseAsync();
                await _sharedContext.DisposeAsync();
                _sharedContext = null;

                logger.LogDebug("共享上下文已关闭并释放。");
            }

            if (_sharedBrowser is not null)
            {
                await _sharedBrowser.CloseAsync();
                await _sharedBrowser.DisposeAsync();
                _sharedBrowser = null;

                logger.LogDebug("共享浏览器实例已关闭并释放。");
            }

            if (_playwright is not null)
            {
                _playwright.Dispose();
                _playwright = null;

                logger.LogDebug("Playwright 引擎已注销。");
            }

            logger.LogInformation("浏览器服务已彻底关闭。");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "关闭浏览器服务时发生非预期错误。");
        }
    }
}