using SpotifyAPI.Web;

namespace Core.Spotify.Auth.Storage;

public interface ITokensRepository
{
    Task<(long UserId, AuthorizationCodeTokenResponse token)[]> ReadAllAsync();
    Task<AuthorizationCodeTokenResponse?> TryReadAsync(long userId);
    Task CreateOrUpdateAsync(long userId, AuthorizationCodeTokenResponse token);
}