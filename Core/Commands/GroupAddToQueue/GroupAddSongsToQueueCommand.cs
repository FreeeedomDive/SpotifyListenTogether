using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Links;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Dto.Spotify;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

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
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<GroupAddSongsToQueueCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;

    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var tasks = Message.Split("\n").Select(x => spotifyLinksRecognizeService.TryRecognizeAsync(x));
        var spotifyLinks = await Task.WhenAll(tasks);
        var tracksUris = spotifyLinks.Where(x => x?.Type == SpotifyLinkType.Track).Select(x => x!.Id.ToTrackUri()).ToArray();

        var result = await this.ApplyToAllParticipants(
            async (profileId, participant) =>
            {
                foreach (var uri in tracksUris)
                {
                    await SpotifyHelpersApiClient.PlayerV2.AddToQueueAsync(
                        profileId,
                        new SpotifyQueueRequestDto
                        {
                            Uri = uri,
                            DeviceId = participant.DeviceId,
                        }
                    );
                }
            }, Logger
        );

        await NotifyAllAsync(
            Session, $"{UserName} добавляет в очередь {tracksUris.Length.ToPluralizedString("трек", "трека", "треков")}\n"
                     + result.ToFormattedString(), ParseMode.MarkdownV2
        );
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}
