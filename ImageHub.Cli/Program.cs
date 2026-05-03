using Flurl.Http;
using ImageHub;
using ImageHub.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright;
using Serilog;
using Spectre.Console;
using System.Text;
using System.Text.Json.Nodes;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = BuildHost(args);

        await host.StartAsync();

        // 无参数执行, 进入交互式命令行
        if (args.Length <= 0)
        {
            await InteractiveAsync(host.Services);
        }
        else
        {
            await TriggeredAsync(host.Services);
        }

        await host.StopAsync();
    }


    private static async Task DownloadTask(IServiceProvider service, string url)
    {
        var engine = service.GetRequiredService<IJobService>();

        // 在 Progress 容器中运行
        await AnsiConsole.Progress()
            .Columns(
            [
                new TaskDescriptionColumn(),    // 描述
                new ProgressBarColumn(),        // 进度条
                new PercentageColumn(),         // 百分比
                new SpinnerColumn(),            // 加载动画
            ])
            .StartAsync(async ctx =>
            {
                var task = ctx.AddTask($"[green]解析中:[/] {url}");

                // 关键设置：进入“不确定”状态
                task.IsIndeterminate = true;

                // 执行你的异步任务
                await engine.CreateAsync(url);

                // 任务完成后
                task.IsIndeterminate = false; // 恢复正常
                task.Value = 100;             // 填满
                task.Description = $"[grey]已完成:[/] {url}";
            });
    }


    // 交互式命令
    private static async Task InteractiveAsync(IServiceProvider service)
    {
        Console.Title = "ImageHub Cli";
        var scopeFactory = service.GetRequiredService<IServiceScopeFactory>();

        while (true)
        {
            var url = AnsiConsole.Ask<string>("[bold blue] 请输入网址>>> [/]");
            if (url == "exit") break;


            await using var scope = scopeFactory.CreateAsyncScope();
            var job_service = scope.ServiceProvider.GetRequiredService<IJobService>();

            _ = job_service.CreateAsync(url);
        }
    }


    // 触发式命令
    private static Task TriggeredAsync(IServiceProvider service)
    {
        return Task.CompletedTask;
    }

    // 构建主机
    private static IHost BuildHost(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
#if DEBUG
            .UseEnvironment("development")
#endif
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
            })
            .UseSerilog((context, services, configuration) =>
            {
                configuration.ReadFrom.Configuration(context.Configuration);
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;

                services.AddImageHub(opts =>
                {
                    var connect_string = configuration.GetConnectionString("Sqlite");
                    var browser_selection = configuration.GetSection("Browser");
                    var resource_selection = configuration.GetSection("Resource");
                    var tg_bot_selection = configuration.GetSection("Telegram");
                    var source_concurrency_selection = configuration.GetSection("SourceConcurrency");

                    opts.Database(x => x.UseSqlite(connect_string));
                    opts.Browser(x => browser_selection.Bind(x));
                    opts.Resource(x => resource_selection.Bind(x));
                    opts.TelegramBot(x => tg_bot_selection.Bind(x));
                    opts.Source(x => source_concurrency_selection.Bind(x));
                });
            })
            .Build();

        return host;
    }
}