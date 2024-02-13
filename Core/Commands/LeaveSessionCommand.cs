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

    public Session Session { get; set; }

    protected override async Task ExecuteAsync()
    {
        SessionsService.Leave(Session.Id, UserId);
        await NotifyAllAsync(
            $"{UserName} выходит из комнаты\n"
            + $"В этой комнате {Session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
        );
        await SendResponseAsync(UserId, $"Ты покинул комнату `{Session.Id}`", ParseMode.MarkdownV2);
    }
}