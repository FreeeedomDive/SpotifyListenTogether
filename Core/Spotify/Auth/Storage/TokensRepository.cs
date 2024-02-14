using SqlRepositoryBase.Core.Repository;

namespace Core.Spotify.Auth.Storage;

public class TokensRepository : ITokensRepository
{
    public TokensRepository(ISqlRepository<TokenStorageElement> sqlRepository)
    {
        this.sqlRepository = sqlRepository;
    }

    public async Task<string?> TryReadAsync(long userId)
    {
        var result = await sqlRepository.FindAsync(x => x.UserId == userId);
        return result.FirstOrDefault()?.Token;
    }

    public async Task CreateOrUpdateAsync(long userId, string token)
    {
        var existing = (await sqlRepository.FindAsync(x => x.UserId == userId)).FirstOrDefault();
        if (existing is null)
        {
            await sqlRepository.CreateAsync(
                new TokenStorageElement
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    Token = token,
                }
            );
            return;
        }

        await sqlRepository.UpdateAsync(existing.Id, x => x.Token = token);
    }

    private readonly ISqlRepository<TokenStorageElement> sqlRepository;
}