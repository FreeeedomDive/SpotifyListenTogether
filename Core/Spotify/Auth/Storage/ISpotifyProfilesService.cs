namespace Core.Spotify.Auth.Storage;

public interface ISpotifyProfilesService
{
    Task<Guid?> TryGetAuthorizedProfileIdAsync(long telegramUserId);
    Task<Guid> GetOrCreateProfileIdAsync(long telegramUserId);
    Task<bool> IsAuthorizedAsync(Guid profileId);
    Task<Dictionary<long, Guid>> ReadAuthorizedProfileIdsAsync(IEnumerable<long> telegramUserIds);
}
