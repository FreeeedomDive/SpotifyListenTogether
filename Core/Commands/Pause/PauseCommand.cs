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

namespace Core.Commands.Pause;

public class PauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ICommandCanSaveSpotifyDeviceId, IPauseCommand
{
    public PauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<PauseCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var playback = await SpotifyClient.Player.GetCurrentPlayback();
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.PausePlayback();
                await this.SaveDeviceIdAsync(client, participant);
            }, Logger
        );
        await NotifyAllAsync(Session, $"{UserName} ставит воспроизведение на паузу\n{result.ToFormattedString()}");
        try
        {
            if (playback.Context is null)
            {
                return;
            }

            Session.Context = new SessionContext
            {
                ContextUri = playback.Context.Uri,
                TrackUri = (playback.Item as FullTrack)!.Uri,
                PositionMs = playback.ProgressMs,
            };
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Failed to save context");
        }
    }
}