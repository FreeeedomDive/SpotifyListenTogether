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

public class PauseCommand : CommandBase, ICommandWithSpotifyAuth, ICommandForAllParticipants, ICommandCanSaveSpotifyDeviceId
{
    public PauseCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
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
    }
}