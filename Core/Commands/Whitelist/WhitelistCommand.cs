using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Whitelist;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.Whitelist;

public class WhitelistCommand : CommandBase, IWhitelistCommand
{
    public WhitelistCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
    {
        this.whitelistService = whitelistService;
    }

    protected override async Task ExecuteAsync()
    {
        await whitelistService.AddToWhitelistAsync(UserId);
    }

    private readonly IWhitelistService whitelistService;
}