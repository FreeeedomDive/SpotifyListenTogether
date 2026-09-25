using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Auth.Storage;
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
        ILogger<ForceAuthCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, logger)
    {
    }

    protected override Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}
