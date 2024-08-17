using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.Whitelist;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.GroupAddToQueue;

public class GroupAddSongsToQueueCommand
    : CommandBase,
      ICommandWithSpotifyAuth,
      ICommandCanSaveSpotifyDeviceId,
      ICommandWithAliveDeviceValidation,
      IGroupAddSongsToQueueCommand
{
    public GroupAddSongsToQueueCommand(
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
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
        var tracksUris = spotifyLinks.Where(x => x?.Type == SpotifyLinkType.Track).Select(x => x!.Id.ToTrackUri()).ToArray();

        var result = await this.ApplyToAllParticipants(
            async (spotifyClient, participant) =>
            {
                foreach (var uri in tracksUris)
                {
                    await spotifyClient.Player.AddToQueue(
                        new PlayerAddToQueueRequest(uri)
                        {
                            DeviceId = participant.DeviceId,
                        }
                    );
                }
            }, LoggerClient
        );

        await NotifyAllAsync(
            Session, $"{UserName} добавляет в очередь {tracksUris.Length.ToPluralizedString("трек", "трека", "треков")}\n"
                     + result.ToFormattedString(), ParseMode.MarkdownV2
        );
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}