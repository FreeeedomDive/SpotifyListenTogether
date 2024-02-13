using Telegram.Bot.Types;

namespace Core.Commands.Recognize;

public interface ICommandsRecognizer
{
    CommandType? ParseCommand(Message message);
}