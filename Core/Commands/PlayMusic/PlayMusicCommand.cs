using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.PlayMusic;

public class PlayMusicCommand
    : CommandBase,
      ICommandWithSpotifyAuth,
      ICommandCanSaveSpotifyDeviceId,
      ICommandWithAliveDeviceValidation,
      IPlayMusicCommand
{
    public PlayMusicCommand(
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<PlayMusicCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
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

            await PlayTrackAsync(track.Id, track);
            return;
        }

        switch (spotifyLink.Type)
        {
            case SpotifyLinkType.Track:
                var track = await SpotifyClient.Tracks.TryGet(spotifyLink.Id);
                await PlayTrackAsync(spotifyLink.Id, track);
                break;
            case SpotifyLinkType.Album:
                var album = await SpotifyClient.Albums.TryGet(spotifyLink.Id);
                await PlayAlbumAsync(spotifyLink.Id, album);
                break;
            case SpotifyLinkType.Playlist:
                var playlist = await SpotifyClient.Playlists.TryGet(spotifyLink.Id);
                await PlayPlaylistAsContextAsync(spotifyLink.Id, playlist);
                break;
            case SpotifyLinkType.Artist:
                await SendResponseAsync(UserId, "Воспроизведение исполнителей не поддерживается, советуем найти плейлист с этим исполнителем и воспроизвести его.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task PlayTrackAsync(string trackId, FullTrack? track = null)
    {
        var trackLink = track?.ExternalUrls["spotify"] ?? $"https://open.spotify.com/track/{trackId}";
        var trackUri = track?.Uri ?? trackId.ToTrackUri();
        var shouldAddToQueue = await ShouldAddToQueueAsync();
        var result = shouldAddToQueue
            ? await this.ApplyToAllParticipants((client, _) => client.Player.AddToQueue(new PlayerAddToQueueRequest(trackUri)), Logger)
            : await this.ApplyToAllParticipants(
                async (client, participant) =>
                {
                    await client.Player.ResumePlayback(
                        new PlayerResumePlaybackRequest
                        {
                            Uris = new List<string>
                            {
                                trackUri,
                            },
                            DeviceId = participant.DeviceId,
                        }
                    );
                    await this.SaveDeviceIdAsync(client, participant);
                }, Logger
            );

        var text = track is null
            ? $"[трек]({trackLink})"
            : track.ToFormattedString();
        await NotifyAllAsync(
            Session, $"{UserName} добавляет в очередь {text}\n{result.ToFormattedString()}", ParseMode.MarkdownV2
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

    private async Task PlayAlbumAsync(string albumId, FullAlbum? album = null)
    {
        var albumLink = album?.ExternalUrls["spotify"] ?? $"https://open.spotify.com/album/{albumId}";
        var albumUri = album?.Uri ?? $"spotify:album:{albumId}";
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = albumUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant);
            }, Logger
        );
        var albumText = album is null
            ? $"[альбома]({albumLink})"
            : $"альбома {album.ToFormattedString()}";
        await NotifyAllAsync(
            Session, $"{UserName} начинает воспроизведение {albumText}\n{result.ToFormattedString()}",
            ParseMode.MarkdownV2
        );
    }

    private async Task PlayPlaylistAsContextAsync(string playlistId, FullPlaylist? playlist = null)
    {
        var playlistLink = playlist?.ExternalUrls?["spotify"] ?? $"https://open.spotify.com/playlist/{playlistId}";
        var playlistUri = playlist?.Uri ?? $"spotify:playlist:{playlistId}";
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = playlistUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant);
            }, Logger
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