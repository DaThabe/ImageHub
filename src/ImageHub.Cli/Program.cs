using ImageHub.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Spectre.Console;
using ThabeSoft.Mediator;

namespace ImageHub.Cli;

internal static class Program
{
    private static async Task Main(string[] args)
    {
        var host = BuildHost(args);

        await host.StartAsync();

        // 无参数执行, 进入交互式命令行
        if (args.Length == 0)
        {
            await InteractiveAsync(host.Services);
        }
        else
        {
            await TriggeredAsync(host.Services);
        }

        await host.StopAsync();
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
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await sender.SendAsync(new CreateJobCommand(url));
        }
    }


    // 触发式命令
    private static Task TriggeredAsync(IServiceProvider _)
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
                logging.ClearProviders())
            .UseSerilog((context, _, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration))
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