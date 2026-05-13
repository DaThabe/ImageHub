using Microsoft.Extensions.Configuration;

namespace ImageHub.Infrastructure.Telegram;


/// <summary>
/// 机器人配置
/// </summary>
public sealed record TelegramBotOptions
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


    public override string ToString()
    {
        return ChatId.ToString();
    }
}
