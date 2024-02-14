using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Client;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class CreateSessionCommand : CommandBase, ICommandWithoutSession, IInitiateSpotifyAuthCommand
{
    public CreateSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
    {
    }

    protected override async Task ExecuteAsync()
    {
        var newSessionId = SessionsService.Create(
            new SessionParticipant
            {
                UserId = UserId,
                UserName = UserName,
            }
        );
        await SendResponseAsync(UserId, $"Создана комната `{newSessionId}`", ParseMode.MarkdownV2);
    }
}