using Telegram.Bot.Types;

namespace Core.Commands.Base;

public interface ICommandBase
{
    Task ExecuteAsync(Message message);
}