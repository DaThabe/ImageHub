using ImageHub.Application;
using ImageHub.Application.Jobs;
using ImageHub.Application.Metadatas;
using ImageHub.Application.Publishs;
using ImageHub.Domain.Events;
using ImageHub.Domain.Repositories;
using ImageHub.Domain.Services;
using ImageHub.Events;
using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Database;
using ImageHub.Infrastructure.Repositories;
using ImageHub.Infrastructure.Services.Events;
using ImageHub.Infrastructure.Services.Jobs;
using ImageHub.Infrastructure.Services.Metadatas;
using ImageHub.Infrastructure.Services.Publishs;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Infrastructure.Services.Sources;
using ImageHub.Infrastructure.Telegram;
using ImageHub.Publishers.Telegram;
using ImageHub.Services;
using ImageHub.Sources;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        /// <param name="databaseBuildAction"></param>
        /// <returns></returns>
        public IServiceCollection AddImageHub(Action<ImageHubOptionsBuilder> optionsAction)
        {
            var optionsBuilder = new ImageHubOptionsBuilder();
            optionsAction.Invoke(optionsBuilder);

            // 配置
            services.Configure(optionsBuilder.BrowserOptionsBuildAction);
            services.Configure(optionsBuilder.TelegramBotOptionsBuildAction);
            services.Configure(optionsBuilder.ResourceOptionsBuildAction);
            services.Configure(optionsBuilder.SourceConcurrencyOptionsBuildAction);


            // 浏览器
            services.AddSingleton<IBrowserService, BrowserService>();

            // 电报机器人
            services.AddSingleton<ITelegramBotClient>(x =>
            {
                var options = x.GetRequiredService<IOptions<TelegramBotOptions>>();
                return new TelegramBotClient(options.Value.BotToken);
            });
            // 数据库
            services.AddDbContext<ImageHubDbContext>(optionsBuilder.DbContextOptionsBuildAction);
            services.AddScoped<IUnitOfWork, UnitOfWork>();

           
            // 任务
            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobService, JobService>();
            services.AddScoped<IJobProcessor, JobProcessor>();
            services.AddSingleton<JobConsumer>();
            
            // 来源
            services.AddScoped<ISourceRepository, SourceRepository>();
            services.AddSingleton<ISourceParser, SourceParser>();
            services.AddSingleton<ISourceSemaphoreSlim, SourceSemaphoreSlim>();

            // 元数据
            services.AddScoped<IMetadataRepository, MetadataRepository>();
            services.AddSingleton<IMetadataOrchestrator, MetadataOrchestrator>();
            services.AddSingleton<IMetadataExtractor, PixivMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, XiaoHongShuMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, TwitterMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, WeiboMetadataExtractor>();

            // 资源
            services.AddScoped<IResourceRepository, ResourceRepository>();
            services.AddSingleton<IResourceDownloader, ResourceDownloader>();

            // 发布
            services.AddScoped<IPublishTargetRepository, PublishTargetRepository>();
            services.AddScoped<IPublishTargetService, PublishTargetService>();
            services.AddScoped<IPublishJobRepository, PublishJobRepository>();
            services.AddScoped<IPublishJobService, PublishJobService>();


            // 事件
            services.AddSingleton<IDomainEventPublisher, DomainEventDispatcher>();
            services.AddSingleton<IDomainEventHandler<JobResourcesReadyDomainEvent>, TelegramPublisher>();
            services.AddSingleton<IDomainEventHandler<JobCreatedDomainEvent>>(x => x.GetRequiredService<JobConsumer>());


            // 后台任务
            services.AddHostedService<DbMigrateHostedService>();
            services.AddHostedService<TelegramBotHostedService>();
            services.AddHostedService(x => x.GetRequiredService<IBrowserService>());
            services.AddHostedService<PublishTargetInitializer>();
            services.AddHostedService(x => x.GetRequiredService<JobConsumer>());
            services.AddHostedService<JobRecoveryBackgroundService>();

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