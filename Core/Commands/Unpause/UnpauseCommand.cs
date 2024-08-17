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

namespace Core.Commands.Unpause;

public class UnpauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandWithAliveDeviceValidation, IUnpauseCommand
{
    public UnpauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var playerResumePlaybackRequest = new PlayerResumePlaybackRequest();
        if (Session.Context?.ContextUri is not null)
        {
            playerResumePlaybackRequest.ContextUri = Session.Context.ContextUri;
            playerResumePlaybackRequest.PositionMs = Session.Context.PositionMs;
            playerResumePlaybackRequest.OffsetParam = new PlayerResumePlaybackRequest.Offset
            {
                Uri = Session.Context.TrackUri,
            };
        }

        var result = await this.ApplyToAllParticipants(
            (client, participant) =>
            {
                playerResumePlaybackRequest.DeviceId = participant.DeviceId;
                return client.Player.ResumePlayback(playerResumePlaybackRequest);
            }, LoggerClient
        );
        await NotifyAllAsync(Session, $"{UserName} возобновляет воспроизведение\n{result.ToFormattedString()}");
    }
}