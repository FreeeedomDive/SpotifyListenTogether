namespace Core.Spotify.Auth.Storage;

public interface ITokensRepository
{
    Task<string?> TryReadAsync(long userId);
    Task CreateOrUpdateAsync(long userId, string token);
}