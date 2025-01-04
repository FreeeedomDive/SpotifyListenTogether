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

namespace Core.Commands.ForceSync;

public class ForceSyncCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, IForceSyncCommand
{
    public ForceSyncCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<ForceSyncCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var allCurrentProgress = await Task.WhenAll(
            UserIdToSpotifyClient.Values.Select(
                async x => (await x.SpotifyClient.Player.GetCurrentPlayback()).ProgressMs
            )
        );
        var minProgress = allCurrentProgress.Min();
        var result = await this.ApplyToAllParticipants(
            (client, _) => client.Player.SeekTo(new PlayerSeekToRequest(minProgress)), Logger
        );
        await NotifyAllAsync(Session, $"{UserName} сбрасывает прогресс воспроизведения трека до {minProgress} мс\n{result.ToFormattedString()}");
    }
}