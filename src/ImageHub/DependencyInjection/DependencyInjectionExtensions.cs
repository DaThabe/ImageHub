using ImageHub.Application.Services;
using ImageHub.DependencyInjection;
using ImageHub.Domain.Entities;
using ImageHub.Infrastructure.Browser;
using ImageHub.Infrastructure.Database;
using ImageHub.Infrastructure.Services.Jobs;
using ImageHub.Infrastructure.Services.Metadatas;
using ImageHub.Infrastructure.Services.Publishs;
using ImageHub.Infrastructure.Services.Resources;
using ImageHub.Infrastructure.Services.Sources;
using ImageHub.Infrastructure.Telegram;
using ImageHub.Models;
using ImageHub.Sources;
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
        public IServiceCollection AddImageHub(Action<IImageHubOptions> optionsAction)
        {
            var optionsBuilder = new ImageHubOptions();
            optionsAction.Invoke(optionsBuilder);

            // 配置
            services.Configure(optionsBuilder.BrowserOptionsBuildAction);
            services.Configure(optionsBuilder.TelegramBotOptionsBuildAction);
            services.Configure(optionsBuilder.ResourceOptionsBuildAction);
            services.Configure(optionsBuilder.SourceConcurrencyOptionsBuildAction);

            // 中介者
            services.AddMediator();
            services.AddDomainEventPublisher();

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
            services.AddDbContext<ImageHubDbContext>(optionsBuilder.DbContextOptionsBuildAction);
            services.AddHostedService<DbMigrateHostedService>();
            services.AddEfCorePersistence<ImageHubDbContext>(x =>
            {
                x.AddRepository<Job, JobId>();
                x.AddRepository<Source, SourceId>();
                x.AddRepository<Metadata, MetadataId>();
                x.AddRepository<Resource, ResourceId>();
                x.AddRepository<PublishTarget, PublishTargetId>();
                x.AddRepository<PublishJob, PublishJobId>();
            });

            // 任务
            services.AddScoped<IJobProcessor, JobProcessor>();
            services.AddSingleton<IJobQueue, JobQueue>();
            services.AddHostedService<JobConsumerBackgroundService>();

            // 来源
            services.AddSingleton<ISourceParser, SourceParser>();
            services.AddSingleton<ISourceSemaphoreSlim, SourceSemaphoreSlim>();

            // 元数据
            services.AddSingleton<IMetadataProvider, MetadataProvider>();
            services.AddSingleton<IMetadataExtractor, PixivMetadataExtractor>();
            //services.AddSingleton<IMetadataExtractor, XiaoHongShuMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, TwitterMetadataExtractor>();
            services.AddSingleton<IMetadataExtractor, WeiboMetadataExtractor>();

            // 资源
            services.AddSingleton<IResourceDownloader, ResourceDownloader>();

            // 发布
            services.AddScoped<IPublishTargetService, PublishTargetService>();
            services.AddScoped<IPublishJobService, PublishJobService>();
            services.AddHostedService<PublishTargetInitializer>();

            return services;
        }
    }
}