using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public interface ISpotifyClientFactory
{
    Task InitializeAllSavedClientsAsync();
    Task<ISpotifyClient?> GetAsync(long telegramUserId);
    Task<ISpotifyClient?> CreateOrGetAsync(long telegramUserId, bool forceReAuth = false);
}