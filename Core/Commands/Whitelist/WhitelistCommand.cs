using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
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
        ILogger<WhitelistCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
        this.whitelistService = whitelistService;
    }

    protected override async Task ExecuteAsync()
    {
        await whitelistService.AddToWhitelistAsync(UserId);
    }

    private readonly IWhitelistService whitelistService;
}