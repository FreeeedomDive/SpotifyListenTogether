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

public class PlayMusicCommand
    : CommandBase,
      ICommandWithSpotifyAuth,
      ICommandCanSaveSpotifyDeviceId,
      ICommandWithAliveDeviceValidation
{
    public PlayMusicCommand(
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
        var spotifyLink = await spotifyLinksRecognizeService.TryRecognizeAsync(Message);
        if (spotifyLink is null)
        {
            var searchResponse = await SpotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, Message));
            var track = searchResponse.Tracks.Items?.FirstOrDefault();
            if (track is null)
            {
                await SendResponseAsync(UserId, "Ничего не найдено");
                return;
            }

            await PlayTrackAsync(track);
            return;
        }

        switch (spotifyLink.Type)
        {
            case SpotifyLinkType.Track:
                var track = await SpotifyClient.Tracks.Get(spotifyLink.Id);
                await PlayTrackAsync(track);
                break;
            case SpotifyLinkType.Artist:
                await SendResponseAsync(UserId, "Воспроизведение исполнителей не поддерживается, советуем найти плейлист с этим исполнителем и воспроизвести его.");
                break;
            case SpotifyLinkType.Album:
                var album = await SpotifyClient.Albums.Get(spotifyLink.Id);
                await PlayAlbumAsync(album);
                break;
            case SpotifyLinkType.Playlist:
                // TODO: остановлено до лучших времен, когда метод получения плейлиста перестанет отдавать 403
                // var playlist = FetchPlaylist ? await SpotifyClient.Playlists.Get(spotifyLink.Id) : null;
                await PlayPlaylistAsContextAsync(null, spotifyLink.Id);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task PlayTrackAsync(FullTrack track)
    {
        var shouldAddToQueue = await ShouldAddToQueueAsync();
        var result = shouldAddToQueue
            ? await this.ApplyToAllParticipants((client, _) => client.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri)), LoggerClient)
            : await this.ApplyToAllParticipants(
                async (client, participant) =>
                {
                    await client.Player.ResumePlayback(
                        new PlayerResumePlaybackRequest
                        {
                            Uris = new List<string>
                            {
                                track.Uri,
                            },
                            DeviceId = participant.DeviceId,
                        }
                    );
                    await this.SaveDeviceIdAsync(client, participant, true);
                }, LoggerClient
            );

        await NotifyAllAsync(
            Session, $"{UserName} добавляет в очередь {track.ToFormattedString()}\n{result.ToFormattedString()}", ParseMode.MarkdownV2
        );
    }

    private async Task<bool> ShouldAddToQueueAsync()
    {
        return (await Task.WhenAll(
                   UserIdToSpotifyClient.Select(x => x.Value.SpotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest()))
               ))
               .Select(x => x?.Item is not FullTrack fullTrack ? null : fullTrack.Id)
               .Distinct()
               .Count() == 1;
    }

    private async Task PlayAlbumAsync(FullAlbum album)
    {
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = album.Uri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant, true);
            }, LoggerClient
        );
        await NotifyAllAsync(
            Session, $"{UserName} начинает воспроизведение альбома {album.ToFormattedString()}\n{result.ToFormattedString()}",
            ParseMode.MarkdownV2
        );
    }

    private async Task PlayPlaylistAsContextAsync(FullPlaylist? playlist, string playlistId)
    {
        var playlistLink = $"https://open.spotify.com/playlist/{playlistId}";
        var playlistUri = $"spotify:playlist:{playlistId}";
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = playlist?.Uri ?? playlistUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant, true);
            }, LoggerClient
        );
        var playlistText = playlist is null
            ? $"[плейлиста]({playlistLink})"
            : $"плейлиста {playlist.ToFormattedString()}";
        await NotifyAllAsync(
            Session, $"{UserName} начинает воспроизведение {playlistText}\n{result.ToFormattedString()}",
            ParseMode.MarkdownV2
        );
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}