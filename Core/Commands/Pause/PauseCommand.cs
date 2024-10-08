using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using SpotifyAPI.Web;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.Pause;

public class PauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ICommandCanSaveSpotifyDeviceId, IPauseCommand
{
    public PauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
    {
    }

    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public Session Session { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.PausePlayback();
                await this.SaveDeviceIdAsync(client, participant);
            }, LoggerClient
        );
        await NotifyAllAsync(Session, $"{UserName} ставит воспроизведение на паузу\n{result.ToFormattedString()}");
        try
        {
            var playback = await SpotifyClient.Player.GetCurrentPlayback();
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
            await LoggerClient.ErrorAsync(e, "Failed to save context");
        }
    }
}