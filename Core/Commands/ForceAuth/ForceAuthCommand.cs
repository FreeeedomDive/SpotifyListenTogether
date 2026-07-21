using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using Telegram.Bot;

namespace Core.Commands.ForceAuth;

public class ForceAuthCommand : CommandBase, IInitiateSpotifyAuthCommand, IForceAuthCommand
{
    public ForceAuthCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<ForceAuthCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    protected override Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
