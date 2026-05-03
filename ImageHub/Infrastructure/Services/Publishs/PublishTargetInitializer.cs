using ImageHub.Application.Publishs;
using ImageHub.Publishers.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageHub.Infrastructure.Services.Publishs;


public sealed class PublishTargetInitializer(
    IOptions<TelegramBotOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<PublishTargetInitializer> logger
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IPublishTargetService>();
        await service.SetTelegramGroup(options.Value.ChatId, cancellationToken);
        logger.LogInformation("已设置电报群组发布目标, 群号:{number}", options.Value.ChatId);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}