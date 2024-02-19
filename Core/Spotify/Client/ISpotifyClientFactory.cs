using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public interface ISpotifyClientFactory
{
    Task<ISpotifyClient?> CreateOrGetAsync(long telegramUserId, bool forceReAuth = false);
}