using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public class SpotifyClientStorage : ISpotifyClientStorage
{
    public ISpotifyClient? TryRead(long telegramUserId)
    {
        return spotifyClients.GetValueOrDefault(telegramUserId);
    }

    public void CreateOrUpdate(long telegramUserId, ISpotifyClient spotifyClient)
    {
        spotifyClients[telegramUserId] = spotifyClient;
    }

    public void Delete(long telegramUserId)
    {
        spotifyClients.Remove(telegramUserId);
    }

    private readonly Dictionary<long, ISpotifyClient> spotifyClients = new();
}