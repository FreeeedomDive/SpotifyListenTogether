using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Whitelist;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class ForceAuthCommand : CommandBase, IInitiateSpotifyAuthCommand
{
    public ForceAuthCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
    {
    }

    protected override Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}