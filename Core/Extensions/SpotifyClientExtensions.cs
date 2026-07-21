using SpotifyHelpers.Api.Client.Metadata;
using SpotifyHelpers.Dto.Spotify;

namespace Core.Extensions;

public static class SpotifyClientExtensions
{
    public static async Task<SpotifyTrackDto?> TryGetTrackAsync(this IMetadataClient client, string trackId)
    {
        try
        {
            return await client.GetTrackAsync(trackId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<SpotifyAlbumDetailsDto?> TryGetAlbumAsync(this IMetadataClient client, string albumId)
    {
        try
        {
            return await client.GetAlbumAsync(albumId);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public static async Task<SpotifyPlaylistDto?> TryGetPlaylistAsync(this IMetadataClient client, string playlistId)
    {
        try
        {
            return await client.GetPlaylistAsync(playlistId);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
