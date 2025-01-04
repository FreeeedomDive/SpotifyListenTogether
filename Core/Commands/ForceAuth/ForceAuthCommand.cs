using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using Telegram.Bot;

namespace Core.Commands.ForceAuth;

public class ForceAuthCommand : CommandBase, IInitiateSpotifyAuthCommand, IForceAuthCommand
{
    public ForceAuthCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<ForceAuthCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    protected override Task ExecuteAsync()
    {
        return Task.CompletedTask;
    }
}