using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Links;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Dto.Spotify;
using Telegram.Bot;

namespace Core.Commands.StatsByArtists;

public class PlaylistStatsByArtistCommand : CommandBase, IPlaylistStatsByArtistCommand
{
    public PlaylistStatsByArtistCommand(
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<PlaylistStatsByArtistCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
    }

    protected override async Task ExecuteAsync()
    {
        var parts = Message.Split();
        if (parts.Length < 2)
        {
            await SendResponseAsync(UserId, "Нет ссылки на плейлист");
            return;
        }

        var spotifyLink = await spotifyLinksRecognizeService.TryRecognizeAsync(parts[1]);
        if (spotifyLink is null || spotifyLink.Type != SpotifyLinkType.Playlist)
        {
            await SendResponseAsync(UserId, "Некорректная ссылка на плейлист");
            return;
        }

        var tracks = await GetTracksInPlaylistAsync(spotifyLink.Id);
        var artists = tracks
                      .SelectMany(track => track.Artists ?? [])
                      .Where(artist => artist is not null)
                      .GroupBy(artist => artist.Name ?? string.Empty)
                      .Select(group => (Name: group.Key, Count: group.Count()))
                      .OrderByDescending(pair => pair.Count)
                      .Select(pair => $"{pair.Name}: {pair.Count}");

        await SendResponseAsync(UserId, string.Join("\n", artists));
    }

    private async Task<SpotifyTrackDto[]> GetTracksInPlaylistAsync(string playlistId)
    {
        // maximum possible tracks in playlist is 10000
        var total = 10000;
        var offset = 0;
        List<SpotifyTrackDto> tracks = new();
        while (offset < total)
        {
            var currentPaging = await SpotifyHelpersApiClient.Metadata.GetPlaylistItemsAsync(playlistId, offset);
            total = currentPaging.Total;
            var pageItems = currentPaging.Items ?? [];
            var currentPageTracks = pageItems
                                    .Where(x => x?.Track is not null)
                                    .Select(x => x.Track!)
                                    .ToList();
            tracks.AddRange(currentPageTracks);
            offset += pageItems.Length;
            if (pageItems.Length == 0)
            {
                break;
            }
        }

        return tracks.ToArray();
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}
