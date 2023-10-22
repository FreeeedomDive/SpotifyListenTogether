using Telegram.Bot;
using Telegram.Bot.Types;

namespace Core.TelegramWorker;

public interface ITelegramBotWorker
{
    Task StartAsync();
}