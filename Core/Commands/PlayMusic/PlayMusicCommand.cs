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
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<PlayMusicCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
    }

    public Session Session { get; set; } = null!;
    public Dictionary<long, (SessionParticipant Participant, Guid ProfileId)> UserIdToSpotifyProfile { get; set; } = null!;
    public Guid SpotifyProfileId { get; set; }

    protected override async Task ExecuteAsync()
    {
        var spotifyLink = await spotifyLinksRecognizeService.TryRecognizeAsync(Message);
        if (spotifyLink is null)
        {
            var searchResponse = await SpotifyHelpersApiClient.Metadata.SearchTracksAsync(Message);
            var track = searchResponse.Items?.FirstOrDefault();
            if (track?.Id is not { } trackId)
            {
                await SendResponseAsync(UserId, "Ничего не найдено");
                return;
            }

            await PlayTrackAsync(trackId, track);
            return;
        }

        switch (spotifyLink.Type)
        {
            case SpotifyLinkType.Track:
                var track = await SpotifyHelpersApiClient.Metadata.TryGetTrackAsync(spotifyLink.Id);
                await PlayTrackAsync(spotifyLink.Id, track);
                break;
            case SpotifyLinkType.Album:
                var album = await SpotifyHelpersApiClient.Metadata.TryGetAlbumAsync(spotifyLink.Id);
                await PlayAlbumAsync(spotifyLink.Id, album);
                break;
            case SpotifyLinkType.Playlist:
                var playlist = await SpotifyHelpersApiClient.Metadata.TryGetPlaylistAsync(spotifyLink.Id);
                await PlayPlaylistAsContextAsync(spotifyLink.Id, playlist);
                break;
            case SpotifyLinkType.Artist:
                await SendResponseAsync(UserId, "Воспроизведение исполнителей не поддерживается, советуем найти плейлист с этим исполнителем и воспроизвести его.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task PlayTrackAsync(string trackId, SpotifyTrackDto? track = null)
    {
        var trackLink = track?.Url ?? $"https://open.spotify.com/track/{trackId}";
        var trackUri = track?.Uri ?? trackId.ToTrackUri();
        var shouldAddToQueue = await ShouldAddToQueueAsync();
        var result = shouldAddToQueue
            ? await this.ApplyToAllParticipants(
                (profileId, _) => SpotifyHelpersApiClient.PlayerV2.AddToQueueAsync(
                    profileId,
                    new SpotifyQueueRequestDto { Uri = trackUri }),
                Logger)
            : await this.ApplyToAllParticipants(
                async (profileId, participant) =>
                {
                    await SpotifyHelpersApiClient.PlayerV2.PlayAsync(
                        profileId,
                        new SpotifyPlayRequestDto
                        {
                            Uris = [trackUri],
                            DeviceId = participant.DeviceId,
                        }
                    );
                    await this.SaveDeviceIdAsync(SpotifyHelpersApiClient.PlayerV2, profileId, participant);
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
                   UserIdToSpotifyProfile.Values.Select(
                       x => SpotifyHelpersApiClient.PlayerV2.GetCurrentlyPlayingAsync(x.ProfileId))
               ))
               .Select(x => x.Track?.Id)
               .Distinct()
               .Count() == 1;
    }

    private async Task PlayAlbumAsync(string albumId, SpotifyAlbumDetailsDto? album = null)
    {
        var albumLink = album?.Url ?? $"https://open.spotify.com/album/{albumId}";
        var albumUri = album?.Uri ?? $"spotify:album:{albumId}";
        var result = await this.ApplyToAllParticipants(
            async (profileId, participant) =>
            {
                await SpotifyHelpersApiClient.PlayerV2.SetShuffleAsync(
                    profileId,
                    new SpotifyShuffleRequestDto
                    {
                        State = false,
                        DeviceId = participant.DeviceId,
                    });
                await SpotifyHelpersApiClient.PlayerV2.PlayAsync(
                    profileId,
                    new SpotifyPlayRequestDto
                    {
                        ContextUri = albumUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(SpotifyHelpersApiClient.PlayerV2, profileId, participant);
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

    private async Task PlayPlaylistAsContextAsync(string playlistId, SpotifyPlaylistDto? playlist = null)
    {
        var playlistLink = playlist?.Url ?? $"https://open.spotify.com/playlist/{playlistId}";
        var playlistUri = playlist?.Uri ?? $"spotify:playlist:{playlistId}";
        var result = await this.ApplyToAllParticipants(
            async (profileId, participant) =>
            {
                await SpotifyHelpersApiClient.PlayerV2.SetShuffleAsync(
                    profileId,
                    new SpotifyShuffleRequestDto
                    {
                        State = false,
                        DeviceId = participant.DeviceId,
                    });
                await SpotifyHelpersApiClient.PlayerV2.PlayAsync(
                    profileId,
                    new SpotifyPlayRequestDto
                    {
                        ContextUri = playlistUri,
                        DeviceId = participant.DeviceId,
                    }
                );
                await this.SaveDeviceIdAsync(SpotifyHelpersApiClient.PlayerV2, profileId, participant);
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
