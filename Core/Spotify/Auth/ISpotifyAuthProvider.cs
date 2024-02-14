using SpotifyAPI.Web;

namespace Core.Spotify.Auth;

public interface ISpotifyAuthProvider
{
    Task<string> CreateAuthLinkAsync();
    Task<SpotifyClient?> WaitForClientInitializationAsync();
    Task<string?> WaitForTokenAsync();
}