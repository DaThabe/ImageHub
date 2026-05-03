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
        logger.LogInformation("Telegram 机器人服务已启动。账号: @{Username}", me.Username);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogWarning("正在停止机器人服务...");
        await _receiveCTS.CancelAsync();
        _receiveCTS.Dispose();

        await telegramBotClient.Close(cancellationToken);
        logger.LogInformation("<<< 机器人服务已安全关闭。");
    }
}