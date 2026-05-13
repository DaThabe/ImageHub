using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace ImageHub.Infrastructure.Telegram;


internal sealed class TelegramBotHostedService(ITelegramBotClient telegramBotClient, ILogger<TelegramBotHostedService> logger) : IHostedService
{
    private readonly CancellationTokenSource _receiveCTS = new();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var me = await telegramBotClient.GetMe(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Telegram 机器人服务已启动。账号: @{Username}", me?.Username ?? "empty");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _receiveCTS.CancelAsync();
        _receiveCTS.Dispose();
        await telegramBotClient.Close(cancellationToken);

        logger.LogInformation("机器人服务已关闭");
    }
}