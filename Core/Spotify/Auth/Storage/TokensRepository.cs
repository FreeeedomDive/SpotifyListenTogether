using Newtonsoft.Json;
using SpotifyAPI.Web;
using SqlRepositoryBase.Core.Repository;

namespace Core.Spotify.Auth.Storage;

public class TokensRepository : ITokensRepository
{
    public TokensRepository(ISqlRepository<TokenStorageElement> sqlRepository)
    {
        this.sqlRepository = sqlRepository;
    }

    public async Task<(long UserId, AuthorizationCodeTokenResponse token)[]> ReadAllAsync()
    {
        var results = await sqlRepository.ReadAllAsync();
        return results.Select(x => (x.UserId, JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(x.Token)!)).ToArray();
    }

    public async Task<AuthorizationCodeTokenResponse?> TryReadAsync(long userId)
    {
        var result = await sqlRepository.FindAsync(x => x.UserId == userId);
        var token = result.FirstOrDefault()?.Token;
        return token is null ? null : JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(token)!;
    }

    public async Task CreateOrUpdateAsync(long userId, AuthorizationCodeTokenResponse token)
    {
        var jsonToken = JsonConvert.SerializeObject(token, Formatting.Indented);
        var existing = (await sqlRepository.FindAsync(x => x.UserId == userId)).FirstOrDefault();
        if (existing is null)
        {
            await sqlRepository.CreateAsync(
                new TokenStorageElement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = jsonToken,
                }
            );
            return;
        }

        await sqlRepository.UpdateAsync(existing.Id, x => x.Token = jsonToken);
    }

    private readonly ISqlRepository<TokenStorageElement> sqlRepository;
}