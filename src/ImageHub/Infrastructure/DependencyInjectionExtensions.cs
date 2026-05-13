using ImageHub.Application.Services;
using ImageHub.Domain.Events;
using ImageHub.Domain.Repositories;
using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Database;
using ImageHub.Infrastructure.Repositories;
using ImageHub.Infrastructure.Services.Jobs;
using ImageHub.Infrastructure.Services.Metadatas;
using ImageHub.Infrastructure.Services.Publishs;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Infrastructure.Services.Sources;
using ImageHub.Infrastructure.Telegram;
using ImageHub.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.Bot;


#pragma warning disable IDE0130 // 命名空间与文件夹结构不匹配
namespace Microsoft.Extensions.DependencyInjection;
#pragma warning restore IDE0130 // 命名空间与文件夹结构不匹配

public static class DependencyInjectionExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// 添加 ImageHub 相关业务
        /// </summary>
        public IServiceCollection AddImageHub(Action<ImageHubOptionsBuilder> optionsAction)
        {
            var optionsBuilder = new ImageHubOptionsBuilder();
            optionsAction.Invoke(optionsBuilder);

            // 配置
            services.Configure(optionsBuilder.BrowserOptionsBuildAction);
            services.Configure(optionsBuilder.TelegramBotOptionsBuildAction);
            services.Configure(optionsBuilder.ResourceOptionsBuildAction);
            services.Configure(optionsBuilder.SourceConcurrencyOptionsBuildAction);


            // 中介者
            services.AddMediator();
            services.AddMediatorDomainEventPublisher();

            // 浏览器
            services.AddSingleton<IBrowserService, BrowserService>();
            services.AddHostedService(x => x.GetRequiredService<IBrowserService>());
            // 电报机器人
            services.AddSingleton<ITelegramBotClient>(x =>
            {
                var options = x.GetRequiredService<IOptions<TelegramBotOptions>>();
                return new TelegramBotClient(options.Value.BotToken);
            });
            services.AddHostedService<TelegramBotHostedService>();

            // 数据库
            services.AddEfCorePersistence<ImageHubDbContext>(optionsBuilder.DbContextOptionsBuildAction);
            services.AddHostedService<DbMigrateHostedService>();

            // 仓储
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<ISourceRepository, SourceRepository>();
            services.AddScoped<IMetadataRepository, MetadataRepository>();
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddScoped<IPublishTargetRepository, PublishTargetRepository>();

            // 任务
            services.AddScoped<IJobProcessor, JobProcessor>();
            services.AddSingleton<JobQueue>();
            services.AddSingleton<IJobQueue>(x => x.GetRequiredService<JobQueue>());
            services.AddHostedService(x => x.GetRequiredService<JobQueue>());

            // 来源
            services.AddSingleton<ISourceParser, SourceParser>();
            services.AddSingleton<ISourceSemaphoreSlim, SourceSemaphoreSlim>();

            // 元数据
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
            services.AddSingleton<IMetadataExtractor, PixivMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, XiaoHongShuMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, TwitterMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, WeiboMetadataExtractor>();

            // 资源
            services.AddSingleton<IResourceDownloader, ResourceDownloader>();

            // 发布
            services.AddScoped<IPublishTargetService, PublishTargetService>();
            services.AddScoped<IPublishJobRepository, PublishJobRepository>();
            services.AddScoped<IPublishJobService, PublishJobService>();
            services.AddHostedService<PublishTargetInitializer>();

            return services;
        }
    }
}


public class ImageHubOptionsBuilder
{
    internal Action<DbContextOptionsBuilder> DbContextOptionsBuildAction { get; private set; } = delegate { };
    internal Action<BrowserOptions> BrowserOptionsBuildAction { get; private set; } = delegate { };
    internal Action<ResourceOptions> ResourceOptionsBuildAction { get; private set; } = delegate { };
    internal Action<TelegramBotOptions> TelegramBotOptionsBuildAction { get; private set; } = delegate { };
    internal Action<SourceConcurrencyOptions> SourceConcurrencyOptionsBuildAction { get; private set; } = delegate { };


    public ImageHubOptionsBuilder Database(Action<DbContextOptionsBuilder> buildAction)
    {
        DbContextOptionsBuildAction = buildAction;
        return this;
    }
    public ImageHubOptionsBuilder Browser(Action<BrowserOptions> buildAction)
    {
        BrowserOptionsBuildAction = buildAction;
        return this;
    }
    public ImageHubOptionsBuilder Resource(Action<ResourceOptions> buildAction)
    {
        ResourceOptionsBuildAction = buildAction;
        return this;
    }
    public ImageHubOptionsBuilder TelegramBot(Action<TelegramBotOptions> buildAction)
    {
        TelegramBotOptionsBuildAction = buildAction;
        return this;
    }
    public ImageHubOptionsBuilder Source(Action<SourceConcurrencyOptions> buildAction)
    {
        SourceConcurrencyOptionsBuildAction = buildAction;
        return this;
    }
}