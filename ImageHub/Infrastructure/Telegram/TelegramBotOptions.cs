using Microsoft.Extensions.Configuration;

namespace ImageHub.Publishers.Telegram;


/// <summary>
/// 机器人配置
/// </summary>
public class TelegramBotOptions
{
    /// <summary>
    /// 机器人 Api 令牌
    /// </summary>
    [ConfigurationKeyName("BotToken")]
    public string BotToken { get; set; } = string.Empty;

    /// <summary>
    /// 聊天Id
    /// </summary>
    [ConfigurationKeyName("ChatId")]
    public long ChatId { get; set; }
}
