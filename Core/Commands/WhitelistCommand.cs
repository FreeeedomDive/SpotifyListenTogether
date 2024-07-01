using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Whitelist;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class WhitelistCommand : CommandBase
{
    public WhitelistCommand(
        IWhitelistService whitelistService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
    {
        this.whitelistService = whitelistService;
    }

    protected override async Task ExecuteAsync()
    {
        await whitelistService.AddToWhitelistAsync(UserId);
    }

    private readonly IWhitelistService whitelistService;
}