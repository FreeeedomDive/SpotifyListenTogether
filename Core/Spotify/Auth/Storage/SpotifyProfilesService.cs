using Core.Extensions;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Dto.Auth;
using SqlRepositoryBase.Core.Repository;

namespace Core.Spotify.Auth.Storage;

public class SpotifyProfilesService(
    ISqlRepository<AuthApiUsersStorageElement> repository,
    ISpotifyHelpersApiClient spotifyHelpersApiClient
) : ISpotifyProfilesService
{
    public async Task<Guid?> TryGetAuthorizedProfileIdAsync(long telegramUserId)
    {
        var profiles = await ReadAuthorizedProfileIdsAsync([telegramUserId]);
        return profiles.TryGetValue(telegramUserId, out var profileId) ? profileId : null;
    }

    public async Task<Guid> GetOrCreateProfileIdAsync(long telegramUserId)
    {
        var existing = (await repository.FindAsync(x => x.TelegramUserId == telegramUserId)).FirstOrDefault();
        if (existing is not null)
        {
            return existing.Id;
        }

        await ProfileCreationLock.WaitAsync();
        try
        {
            existing = (await repository.FindAsync(x => x.TelegramUserId == telegramUserId)).FirstOrDefault();
            if (existing is not null)
            {
                return existing.Id;
            }

            var profile = new AuthApiUsersStorageElement
            {
                Id = Guid.NewGuid(),
                TelegramUserId = telegramUserId,
            };
            await repository.CreateAsync(profile);
            return profile.Id;
        }
        finally
        {
            ProfileCreationLock.Release();
        }
    }

    public async Task<bool> IsAuthorizedAsync(Guid profileId)
    {
        return await spotifyHelpersApiClient.Auth.TryGetAsync(profileId) is not null;
    }

    public async Task<Dictionary<long, Guid>> ReadAuthorizedProfileIdsAsync(IEnumerable<long> telegramUserIds)
    {
        var userIds = telegramUserIds.Distinct().ToArray();
        if (userIds.Length == 0)
        {
            return [];
        }

        var profiles = await repository.FindAsync(x => userIds.Contains(x.TelegramUserId));
        if (profiles.Length == 0)
        {
            return [];
        }

        var tokens = await spotifyHelpersApiClient.Auth.GetAsync(
            new SearchTokensDto { Ids = profiles.Select(x => x.Id).ToArray() });
        var authorizedIds = tokens.Items
                                  .Where(x => x is not null)
                                  .Select(x => x!.Id)
                                  .ToHashSet();

        return profiles
               .Where(x => authorizedIds.Contains(x.Id))
               .GroupBy(x => x.TelegramUserId)
               .Select(x => x.First())
               .ToDictionary(x => x.TelegramUserId, x => x.Id);
    }

    private static readonly SemaphoreSlim ProfileCreationLock = new(1, 1);
}
