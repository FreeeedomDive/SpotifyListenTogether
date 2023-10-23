using RestSharp;

namespace Core.Spotify.Links;

public class SpotifyLinksRecognizeService : ISpotifyLinksRecognizeService
{
    public async Task<SpotifyLink?> TryRecognizeAsync(string link)
    {
        if (!link.StartsWith(OldSpotifyLink) && !link.StartsWith(NewSpotifyLink))
        {
            return null;
        }

        var hasBadSymbols = link.Contains("?si=", StringComparison.Ordinal);
        var maxLinkLength = hasBadSymbols ? link.IndexOf("?si=", StringComparison.Ordinal) : link.Length;
        link = link[..maxLinkLength];
        var linkTypeWithId = link.StartsWith(OldSpotifyLink)
            ? link[OldSpotifyLink.Length..]
            : await GetIdFromNewLinkAsync(link);

        const string trackType = "track/";
        if (linkTypeWithId.StartsWith(trackType))
        {
            return new SpotifyLink
            {
                Type = SpotifyLinkType.Track,
                Id = linkTypeWithId[trackType.Length..],
            };
        }

        const string artistType = "artist/";
        if (linkTypeWithId.StartsWith(artistType))
        {
            return new SpotifyLink
            {
                Type = SpotifyLinkType.Artist,
                Id = linkTypeWithId[artistType.Length..],
            };
        }

        const string albumType = "album/";
        if (linkTypeWithId.StartsWith(albumType))
        {
            return new SpotifyLink
            {
                Type = SpotifyLinkType.Album,
                Id = linkTypeWithId[albumType.Length..],
            };
        }

        const string playlistType = "playlist/";
        if (linkTypeWithId.StartsWith(playlistType))
        {
            return new SpotifyLink
            {
                Type = SpotifyLinkType.Playlist,
                Id = linkTypeWithId[playlistType.Length..],
            };
        }

        return null;
    }

    private static async Task<string> GetIdFromNewLinkAsync(string link)
    {
        var htmlResponse = (await new RestClient().GetAsync(new RestRequest(link))).Content!;
        const string leftRange = $"href=\"{OldSpotifyLink}";
        const string rightRange = "?si=";
        var leftIndex = htmlResponse.IndexOf(leftRange, StringComparison.Ordinal) + leftRange.Length;
        var rightIndex = htmlResponse.IndexOf(rightRange, StringComparison.Ordinal);
        var id = htmlResponse.Substring(leftIndex, rightIndex - leftIndex);

        return id;
    }

    private const string OldSpotifyLink = "https://open.spotify.com/";
    private const string NewSpotifyLink = "https://spotify.link/";
}