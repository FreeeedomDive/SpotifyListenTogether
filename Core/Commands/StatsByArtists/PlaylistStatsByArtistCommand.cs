using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.Whitelist;
using SpotifyAPI.Web;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.StatsByArtists;

public class PlaylistStatsByArtistCommand : CommandBase, ICommandWithSpotifyAuth, IPlaylistStatsByArtistCommand
{
    public PlaylistStatsByArtistCommand(
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

    public ISpotifyClient SpotifyClient { get; set; } = null!;

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
                      .SelectMany(track => track.Artists)
                      .GroupBy(artist => artist.Name)
                      .Select(group => (Name: group.Key, Count: group.Count()))
                      .OrderByDescending(pair => pair.Count)
                      .Select(pair => $"{pair.Name}: {pair.Count}");

        await SendResponseAsync(UserId, string.Join("\n", artists));
    }

    private async Task<FullTrack[]> GetTracksInPlaylistAsync(string playlistId)
    {
        // maximum possible tracks in playlist is 10000
        var total = 10000;
        List<FullTrack> tracks = new();
        while (tracks.Count < total)
        {
            var currentPaging = await SpotifyClient.Playlists.GetItems(
                playlistId, new PlaylistGetItemsRequest
                {
                    Offset = tracks.Count,
                    Limit = 100,
                }
            );
            total = currentPaging.Total ?? 0;
            var currentPageTracks = currentPaging
                                    .Items!
                                    .Where(x => x.Track is FullTrack)
                                    .Select(x => (x.Track as FullTrack)!)
                                    .ToList();
            tracks.AddRange(currentPageTracks);
        }

        return tracks.ToArray();
    }

    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
}