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
                // TODO: остановлено до лучших времен, когда метод получения трека перестанет отдавать 403
                // var track = await SpotifyClient.Tracks.Get(spotifyLink.Id);
                await PlayTrackAsync(spotifyLink.Id);
                break;
            case SpotifyLinkType.Album:
                // TODO: остановлено до лучших времен, когда метод получения альбома перестанет отдавать 403
                // var album = await SpotifyClient.Albums.Get(spotifyLink.Id);
                await PlayAlbumAsync(spotifyLink.Id);
                break;
            case SpotifyLinkType.Playlist:
                // TODO: остановлено до лучших времен, когда метод получения плейлиста перестанет отдавать 403
                // var playlist = await SpotifyClient.Playlists.Get(spotifyLink.Id);
                await PlayPlaylistAsContextAsync(spotifyLink.Id);
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
        var trackLink = $"https://open.spotify.com/track/{trackId}";
        var shouldAddToQueue = await ShouldAddToQueueAsync();
        var result = shouldAddToQueue
            ? await this.ApplyToAllParticipants((client, _) => client.Player.AddToQueue(new PlayerAddToQueueRequest(trackId.ToTrackUri())), LoggerClient)
            : await this.ApplyToAllParticipants(
                async (client, participant) =>
                {
                    await client.Player.ResumePlayback(
                        new PlayerResumePlaybackRequest
                        {
                            Uris = new List<string>
                            {
                                trackId.ToTrackUri(),
                            },
                            DeviceId = participant.DeviceId,
                        }
                    );
                    await this.SaveDeviceIdAsync(client, participant);
                }, LoggerClient
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
        var albumLink = $"https://open.spotify.com/album/{albumId}";
        var albumUri = $"spotify:album:{albumId}";
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                try
                {
                    await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                }
                catch
                {
                    // SpotifyApi returns unexpected response, but library expects boolean value
                }
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = albumUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant);
            }, LoggerClient
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
        var playlistLink = $"https://open.spotify.com/playlist/{playlistId}";
        var playlistUri = $"spotify:playlist:{playlistId}";
        var result = await this.ApplyToAllParticipants(
            async (client, participant) =>
            {
                try
                {
                    await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                }
                catch
                {
                    // SpotifyApi returns unexpected response, but library expects boolean value
                }
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = playlist?.Uri ?? playlistUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(client, participant);
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