using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public interface ISpotifyClientStorage
{
    ISpotifyClient? TryRead(long telegramUserId);
    void CreateOrUpdate(long telegramUserId, ISpotifyClient spotifyClient);
    void Delete(long telegramUserId);
}