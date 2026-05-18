using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Infrastructure.Services.Sources;
using ImageHub.Infrastructure.Telegram;
using Microsoft.EntityFrameworkCore;

namespace ImageHub.DependencyInjection;


public interface IImageHubOptions
{
    IImageHubOptions Database(Action<DbContextOptionsBuilder> buildAction);
    IImageHubOptions Resource(Action<ResourceOptions> buildAction);
    IImageHubOptions Source(Action<SourceConcurrencyOptions> buildAction);

    IImageHubOptions Browser(Action<BrowserOptions> buildAction);
    IImageHubOptions TelegramBot(Action<TelegramBotOptions> buildAction);
}

internal sealed class ImageHubOptions : IImageHubOptions
{
    public Action<DbContextOptionsBuilder> DbContextOptionsBuildAction { get; private set; } = delegate { };
    public Action<BrowserOptions> BrowserOptionsBuildAction { get; private set; } = delegate { };
    public Action<ResourceOptions> ResourceOptionsBuildAction { get; private set; } = delegate { };
    public Action<TelegramBotOptions> TelegramBotOptionsBuildAction { get; private set; } = delegate { };
    public Action<SourceConcurrencyOptions> SourceConcurrencyOptionsBuildAction { get; private set; } = delegate { };


    IImageHubOptions IImageHubOptions.Database(Action<DbContextOptionsBuilder> buildAction)
    {
        DbContextOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptions IImageHubOptions.Browser(Action<BrowserOptions> buildAction)
    {
        BrowserOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptions IImageHubOptions.Resource(Action<ResourceOptions> buildAction)
    {
        ResourceOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptions IImageHubOptions.TelegramBot(Action<TelegramBotOptions> buildAction)
    {
        TelegramBotOptionsBuildAction = buildAction;
        return this;
    }
    IImageHubOptions IImageHubOptions.Source(Action<SourceConcurrencyOptions> buildAction)
    {
        SourceConcurrencyOptionsBuildAction = buildAction;
        return this;
    }
}