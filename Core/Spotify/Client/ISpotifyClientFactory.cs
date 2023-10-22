using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public interface ISpotifyClientFactory
{
    ISpotifyClient CreateOrGet(long telegramUserId);
}