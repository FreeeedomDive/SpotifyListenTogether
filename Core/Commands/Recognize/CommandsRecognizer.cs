using Core.Extensions;
using Telegram.Bot.Types;

namespace Core.Commands.Recognize;

public class CommandsRecognizer : ICommandsRecognizer
{
    public CommandType? ParseCommand(Message message)
    {
        var messageText = message.Text ?? string.Empty;
        var possibleCommands = new List<CommandType>()
                               .AddIf(messageText.StartsWith("/start"), CommandType.Start)
                               .AddIf(messageText.StartsWith("/create"), CommandType.CreateSession)
                               .AddIf(messageText.StartsWith("/leave"), CommandType.LeaveSession)
                               .AddIf(Guid.TryParse(messageText, out _), CommandType.JoinSession)
                               .AddIf(messageText.StartsWith("/forcesync"), CommandType.ForceSync)
                               .AddIf(messageText.StartsWith("/pause"), CommandType.Pause)
                               .AddIf(messageText.StartsWith("/unpause"), CommandType.Unpause)
                               .AddIf(messageText.StartsWith("/next"), CommandType.NextTrack)
                               .AddIf(messageText.StartsWith("/auth"), CommandType.ForceAuth)
                               .AddIf(messageText.StartsWith("/_wl"), CommandType.Whitelist)
                               .AddIf(messageText.StartsWith("/_session"), CommandType.SessionInfo)
                               .AddIf(messageText.StartsWith("/statsByArtists"), CommandType.StatsByArtists)
                               .AddIf(messageText.Split("\n").Length > 1, CommandType.GroupAddToQueue)
                               .AddIf(true, CommandType.PlayMusic);

        return possibleCommands.FirstOrDefault();
    }
}