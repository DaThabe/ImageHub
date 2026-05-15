using ImageHub.Application.Services;
using ImageHub.Infrastructure.Telegram;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ImageHub.Infrastructure.Services.Publishs;


internal sealed class PublishTargetInitializer(
    IOptions<TelegramBotOptions> options,
    IServiceScopeFactory scopeFactory
    ) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();

        var service = scope.ServiceProvider.GetRequiredService<IPublishTargetService>();
        await service.SetTelegramGroup(options.Value.ChatId, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}