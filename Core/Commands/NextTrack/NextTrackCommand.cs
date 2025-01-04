using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace Core.Commands.NextTrack;

public class NextTrackCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, INextTrackCommand
{
    public NextTrackCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<NextTrackCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var result = await this.ApplyToAllParticipants((client, _) => client.Player.SkipNext(), Logger);
        await NotifyAllAsync(Session, $"{UserName} переключает воспроизведение на следующий трек в очереди\n{result.ToFormattedString()}");
    }
}