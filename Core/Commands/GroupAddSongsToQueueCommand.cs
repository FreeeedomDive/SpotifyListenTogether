using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Spotify.Links;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class GroupAddSongsToQueueCommand
    : CommandBase,
      ICommandWithSpotifyAuth,
      ICommandCanSaveSpotifyDeviceId,
      ICommandWithAliveDeviceValidation
{
    public GroupAddSongsToQueueCommand(
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
    {
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> UserIdToSpotifyClient { get; set; } = null!;

    public ISpotifyClient SpotifyClient { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        var tasks = Message.Split("\n").Select(x => spotifyLinksRecognizeService.TryRecognizeAsync(x));
        var spotifyLinks = await Task.WhenAll(tasks);
        var correctLinks = spotifyLinks.Where(x => x?.Type == SpotifyLinkType.Track).ToArray();

        var result = await this.ApplyToAllParticipants(
            async (spotifyClient, participant) =>
            {
                foreach (var track in correctLinks)
                {
                    await spotifyClient.Player.AddToQueue(new PlayerAddToQueueRequest(track!.Id)
                    {
                        DeviceId = participant.DeviceId,
                    });
                }
            }, LoggerClient
        );

        await NotifyAllAsync(
            Session, $"{UserName} добавляет в очередь {correctLinks.Length} треков\n{result.ToFormattedString()}", ParseMode.MarkdownV2
        );
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}