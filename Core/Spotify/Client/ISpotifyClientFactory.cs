using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public interface ISpotifyClientFactory
{
    Task<ISpotifyClient?> GetAsync(long telegramUserId);
    Task<ISpotifyClient?> CreateOrGetAsync(long telegramUserId, bool forceReAuth = false);
}