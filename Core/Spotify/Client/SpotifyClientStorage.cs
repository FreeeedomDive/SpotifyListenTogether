using SpotifyAPI.Web;

namespace Core.Spotify.Client;

public class SpotifyClientStorage : ISpotifyClientStorage
{
    public ISpotifyClient? TryRead(long telegramUserId)
    {
        return spotifyClients.TryGetValue(telegramUserId, out var spotifyClient) ? spotifyClient : null;
    }

    public void CreateOrUpdate(long telegramUserId, ISpotifyClient spotifyClient)
    {
        spotifyClients[telegramUserId] = spotifyClient;
    }

    private readonly Dictionary<long, ISpotifyClient> spotifyClients = new();
}