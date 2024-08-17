using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.CreateSession;

public class CreateSessionCommand : CommandBase, ICreateSessionCommand, ICommandWithoutSession, IInitiateSpotifyAuthCommand
{
    public CreateSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
    {
    }

    protected override async Task ExecuteAsync()
    {
        var newSessionId = await SessionsService.CreateAsync(
            new SessionParticipant
            {
                UserId = UserId,
                UserName = UserName,
            }
        );
        await SendResponseAsync(UserId, $"Создана комната `{newSessionId}`", ParseMode.MarkdownV2);
    }
}