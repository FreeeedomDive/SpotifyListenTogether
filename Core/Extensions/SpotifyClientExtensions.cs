using SpotifyAPI.Web;

namespace Core.Extensions;

public static class SpotifyClientExtensions
{
    public static async Task<FullTrack?> TryGet(this ITracksClient client, string trackId)
    {
        try
        {
            return await client.Get(trackId);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static async Task<FullAlbum?> TryGet(this IAlbumsClient client, string albumId)
    {
        try
        {
            return await client.Get(albumId);
        }
        catch (Exception e)
        {
            return null;
        }
    }

    public static async Task<FullPlaylist?> TryGet(this IPlaylistsClient client, string playlistId)
    {
        try
        {
            return await client.Get(playlistId);
        }
        catch (Exception e)
        {
            return null;
        }
    }
}