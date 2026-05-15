using ImageHub.Enums;
using ImageHub.Events;
using ImageHub.Infrastructure.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using System.Text;
using System.Text.RegularExpressions;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using ThabeSoft.Mediator;

namespace ImageHub.Publishers.Telegram;


/// <summary>
/// 电报发布器
/// </summary>
internal sealed partial class TelegramPublisher(
    ITelegramBotClient client,
    IOptions<TelegramBotOptions> options,
    ILogger<TelegramPublisher> logger
    ) : INotificationHandler<JobResourcesReadyDomainEvent>
{
    public async ValueTask HandleAsync(JobResourcesReadyDomainEvent @event, CancellationToken cancellationToken = default)
    {
        List<InputMediaPhoto> sentMedias = [];
        var groupCount = GetAverageGroupSize(@event.ResourceFilePaths.Count);

        try
        {
            // 图像分组
            var groups = await GetMediaGroupAsync(@event, cancellationToken);

            // 3. 消息发布阶段
            int groupIndex = 1;
            foreach (var mediaGroup in groups)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    logger.LogDebug("正在发送第 {groupIndex}/{groupCount} 组图像", groupIndex, groups.Count);
                }

                // 发送媒体组
                await client.SendMediaGroup(options.Value.ChatId, mediaGroup, cancellationToken: cancellationToken);

                sentMedias.AddRange(mediaGroup);
                groupIndex++;
            }

            logger.LogInformation("图像发布成功");
        }
        finally
        {
            logger.LogInformation("正在清理图像加载缓存...");

            sentMedias
                .Select(x => x.Media)
                .OfType<InputFileStream>()
                .ToList()
                .ForEach(x => x.Content.Dispose());
        }
    }

    // 转为图像组
    private async Task<List<InputMediaPhoto[]>> GetMediaGroupAsync(JobResourcesReadyDomainEvent @event, CancellationToken cancellationToken = default)
    {
        // 等待任务完成
        var medias = await Task.WhenAll(@event.ResourceFilePaths.Select(x => ProcessSingleImage(x.Value, cancellationToken)));

        // 计算每组数量
        var groupCount = GetAverageGroupSize(medias.Length);

        // 返回结果
        var groups = medias.Where(p => p != null).Select(p => p!).Chunk(groupCount).ToList();

        // 每组第一个元素设置标题
        foreach (var i in groups)
        {
            if (i.FirstOrDefault() is not InputMediaPhoto first) continue;

            first.Caption = ToHtml(@event);
            first.ParseMode = ParseMode.Html;
        }

        return groups;
    }
    // 处理单个文件
    private async Task<InputMediaPhoto?> ProcessSingleImage(string filePath, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileName(filePath);
        var ms = new MemoryStream();
        try
        {
            using var image = await Image.LoadAsync(filePath, cancellationToken);

            if (logger.IsEnabled(LogLevel.Debug))
            {
                logger.LogDebug("正在压缩图像, 文件:{path}", fileName);
            }

            // 缩放并压缩
            await Resize(image).SaveAsJpegAsync(ms, new JpegEncoder { Quality = 65 }, cancellationToken);
            ms.Position = 0;

            // 注意：InputFileStream 会在发送后由 TelegramBot 客户端尝试关闭
            return new InputMediaPhoto(InputFile.FromStream(ms, fileName));
        }
        catch (Exception ex)
        {
            await ms.DisposeAsync();
            logger.LogError(ex, "图像压缩失败: {FileName}", fileName);
            return null;
        }
    }
    // 计算每组图像数量
    private static int GetAverageGroupSize(int n, int max = 10)
    {
        if (n <= max) return n;

        int groups = (int)Math.Ceiling(n / (double)max);
        return (int)Math.Ceiling(n / (double)groups);
    }
    // 缩放图像
    private static Image Resize(Image image, int maxSize = 4096)
    {
        // 原始宽高
        var width = image.Width;
        var height = image.Height;

        // 如果本来就不超过4096，直接返回原图
        if (width <= maxSize && height <= maxSize) return image;

        // 计算缩放比例
        double ratio = Math.Min((double)maxSize / width, (double)maxSize / height);

        int newWidth = (int)(width * ratio);
        int newHeight = (int)(height * ratio);

        // 执行缩放
        image.Mutate(x => x.Resize(newWidth, newHeight));

        return image;
    }




    //格式化为电报可现实的Html文本
    private static string ToHtml(JobResourcesReadyDomainEvent @event)
    {
        StringBuilder sb = new();

        //来源
        sb.Append($"""
            来源: <a href="{@event.SourceUrl}">{FormatImageSourceName(@event.SourceType)}</a>
            """);

        //作者
        if (!string.IsNullOrWhiteSpace(@event.AuthorUrl))
        {
            sb.Append($"""
                {Environment.NewLine}作者: <a href="{@event.AuthorUrl}">{@event.AuthorName ?? "无名称"}</a>
                """);
        }
        else if (!string.IsNullOrWhiteSpace(@event.AuthorName))
        {
            sb.Append($"""
                {Environment.NewLine}作者: {@event.AuthorName}
                """);
        }

        //标题
        if (!string.IsNullOrWhiteSpace(@event.Title))
        {
            sb.Append($"""
                {Environment.NewLine}标题: {@event.Title}
                """);
        }

        //发布时间
        if (@event.UploadAt is not null)
        {
            sb.Append($"""
                {Environment.NewLine}发布时间: {@event.UploadAt.Value.UtcDateTime}
                """);
        }

        //描述
        if (!string.IsNullOrWhiteSpace(@event.Description))
        {
            sb.Append($"""
                {Environment.NewLine}描述: {@event.Description}
                """);
        }

        //标签
        var tags = @event.Tags.Select(FormatToTelegramTag).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (tags.Count > 0)
        {
            sb.Append($"""
                {Environment.NewLine}标签: {string.Concat(tags.Select(x => $"#{x} "))}
                """);
        }

        return sb.ToString();


        // 来源转中文
        static string FormatImageSourceName(SourceType source)
        {
            return source switch
            {
                SourceType.Twitter => "推特",
                SourceType.Pixiv => "Pixiv",
                SourceType.XiaoHongShu => "小红书",
                SourceType.Weibo => "微博",
                _ => "网站"
            };
        }

        // 标签格式化
        static string FormatToTelegramTag(string input)
        {
            // 删除空白
            string noWhitespace = NoWhitespaceRegex().Replace(input, "");

            // 非字母、数字、CJK 统一汉字、日文假名、韩文音节替换为下划线
            string replaced = UnderlineReplaceRegex().Replace(noWhitespace, "_");

            // 合并连续下划线
            string result = CombineUnderlineRegex().Replace(replaced, "_");

            return result.TrimEnd('_');
        }
    }


    [GeneratedRegex(@"\s+")]
    private static partial Regex NoWhitespaceRegex();

    [GeneratedRegex(@"[^\p{L}\p{Nd}_]")]
    private static partial Regex UnderlineReplaceRegex();

    [GeneratedRegex(@"_+")]
    private static partial Regex CombineUnderlineRegex();
}