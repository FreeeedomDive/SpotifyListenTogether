using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using SpotifyAPI.Web;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class UnpauseCommand
    : CommandBase,
      ICommandWithSpotifyAuth,
      ICommandWithAliveDeviceValidation
{
    public UnpauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
    {
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;
    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var result = await this.ApplyToAllParticipants(
            (client, participant) =>
                client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        DeviceId = participant.DeviceId,
                        OffsetParam = new PlayerResumePlaybackRequest.Offset
                        {
                            
                        }
                    }
                ), LoggerClient
        );
        await NotifyAllAsync(Session, $"{UserName} возобновляет воспроизведение\n{result.ToFormattedString()}");
    }
}