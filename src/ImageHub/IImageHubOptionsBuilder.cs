using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Infrastructure.Services.Sources;
using ImageHub.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;

namespace ImageHub;


public interface IImageHubOptionsBuilder
{
    IImageHubOptionsBuilder Database(Action<DbContextOptionsBuilder> buildAction);
    IImageHubOptionsBuilder Resource(Action<ResourceOptions> buildAction);
    IImageHubOptionsBuilder Source(Action<SourceConcurrencyOptions> buildAction);

    IImageHubOptionsBuilder Browser(Action<BrowserOptions> buildAction);
    IImageHubOptionsBuilder TelegramBot(Action<TelegramBotOptions> buildAction);
}


internal sealed class ImageHubOptionsBuilder : IImageHubOptionsBuilder
{
    public Action<DbContextOptionsBuilder> DbContextOptionsBuildAction { get; private set; } = delegate { };
    public Action<BrowserOptions> BrowserOptionsBuildAction { get; private set; } = delegate { };
    public Action<ResourceOptions> ResourceOptionsBuildAction { get; private set; } = delegate { };
    public Action<TelegramBotOptions> TelegramBotOptionsBuildAction { get; private set; } = delegate { };
    public Action<SourceConcurrencyOptions> SourceConcurrencyOptionsBuildAction { get; private set; } = delegate { };


    IImageHubOptionsBuilder IImageHubOptionsBuilder.Database(Action<DbContextOptionsBuilder> buildAction)
    {
        DbContextOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptionsBuilder IImageHubOptionsBuilder.Browser(Action<BrowserOptions> buildAction)
    {
        BrowserOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptionsBuilder IImageHubOptionsBuilder.Resource(Action<ResourceOptions> buildAction)
    {
        ResourceOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptionsBuilder IImageHubOptionsBuilder.TelegramBot(Action<TelegramBotOptions> buildAction)
    {
        TelegramBotOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptionsBuilder IImageHubOptionsBuilder.Source(Action<SourceConcurrencyOptions> buildAction)
    {
        SourceConcurrencyOptionsBuildAction = buildAction;
        return this;
    }
}