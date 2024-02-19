using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class LeaveSessionCommand : CommandBase, ICommandWithSession
{
    public LeaveSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
    {
    }

    public Session Session { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        await SessionsService.LeaveAsync(Session.Id, UserId);
        await NotifyAllAsync(
            Session, $"{UserName} выходит из комнаты\n"
                     + $"В этой комнате {Session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
        );
        var shouldDestroySession = !Session.Participants.Any();
        if (shouldDestroySession)
        {
            await SessionsService.DestroyAsync(Session.Id);
        }

        await SendResponseAsync(
            UserId,
            $"Ты покинул комнату `{Session.Id}`" + (shouldDestroySession ? "\nТы был последним слушателем в этой комнате, она будет удалена" : string.Empty),
            ParseMode.MarkdownV2
        );
    }
}