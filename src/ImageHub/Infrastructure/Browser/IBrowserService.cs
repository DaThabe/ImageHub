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

internal sealed class BrowserService : IBrowserService
{
    private readonly IOptionsMonitor<BrowserOptions> _options;
    private readonly ILogger<BrowserService> _logger;
    private IPlaywright? _playwright;
    private IBrowser? _sharedBrowser;
    private IBrowserContext? _sharedContext;
    private CancellationTokenSource? _cookieReloadCts;

    public BrowserService(IOptionsMonitor<BrowserOptions> options, ILogger<BrowserService> logger)
    {
        _logger = logger;

        _options = options;
        _options.OnChange(OnBrowserOptionsChanged);
    }

    public IPlaywright Playwright => _playwright ?? throw new InvalidOperationException("浏览器服务未启动");
    public IBrowser SharedBrowser => _sharedBrowser ?? throw new InvalidOperationException("浏览器服务未启动");
    public IBrowserContext SharedContext => _sharedContext ?? throw new InvalidOperationException("浏览器服务未启动");


    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("正在启动 Playwright 服务");

        // 初始化 Playwright
        if (_playwright is null)
        {
            _logger.LogDebug("检查并安装 Chromium 内核依赖...");
            //Program.Main(["install", "chromium"]);

            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            _logger.LogDebug("Playwright 引擎实例初始化成功");
        }

        // 初始化 共享浏览器实例
        if (_sharedBrowser is null)
        {
            _sharedBrowser = await _playwright.Chromium.LaunchAsync(_options.CurrentValue);
            _logger.LogDebug("已经创建共享浏览器实例");
        }

        // 初始化 共享上下文
        if (_sharedContext is null)
        {
            int cookieCount = _options.CurrentValue.Cookies?.Count() ?? 0;
            _sharedContext = await _sharedBrowser.NewContextAsync();

            if (cookieCount > 0) await _sharedContext.AddCookiesAsync(_options.CurrentValue.Cookies!);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("已创建共享上下文, 共注入 {cookieCount} 条 Cookie", cookieCount);
            }
        }

        _logger.LogInformation("浏览器服务初始化完成");
    }
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_sharedContext is not null)
        {
            await _sharedContext.CloseAsync();
            await _sharedContext.DisposeAsync();
            _sharedContext = null;
        }

        if (_sharedBrowser is not null)
        {
            await _sharedBrowser.CloseAsync();
            await _sharedBrowser.DisposeAsync();
            _sharedBrowser = null;
        }

        _playwright?.Dispose();
        _playwright = null;

        _logger.LogInformation("浏览器服务已关闭");
    }


    private void OnBrowserOptionsChanged(BrowserOptions options)
    {
        if (_sharedContext is null) return;

        _cookieReloadCts?.Cancel();
        _cookieReloadCts = new();

        Task.Run(async () =>
        {
            await _sharedContext.ClearCookiesAsync();

            var cookies = options.Cookies.ToArray();
            await _sharedContext.AddCookiesAsync(cookies);

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Cookie已更新, {n}条记录", cookies.Length);
            }

        }, _cookieReloadCts.Token);
    }
}