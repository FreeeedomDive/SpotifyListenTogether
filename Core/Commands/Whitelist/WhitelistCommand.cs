using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using Telegram.Bot;

namespace Core.Commands.Whitelist;

public class WhitelistCommand : CommandBase, IWhitelistCommand
{
    public WhitelistCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<WhitelistCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
        this.whitelistService = whitelistService;
    }

    protected override async Task ExecuteAsync()
    {
        await whitelistService.AddToWhitelistAsync(UserId);
    }

    private readonly IWhitelistService whitelistService;
}
