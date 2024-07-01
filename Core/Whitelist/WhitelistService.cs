using SqlRepositoryBase.Core.Repository;

namespace Core.Whitelist;

public class WhitelistService : IWhitelistService
{
    public WhitelistService(ISqlRepository<WhitelistStorageElement> sqlRepository)
    {
        this.sqlRepository = sqlRepository;
    }

    public async Task<bool> IsUserWhitelistedAsync(long userId)
    {
        var result = await sqlRepository.FindAsync(x => x.TelegramUserId == userId);
        return result.Length > 0;
    }

    public async Task AddToWhitelistAsync(long userId)
    {
        if (await IsUserWhitelistedAsync(userId))
        {
            return;
        }

        await sqlRepository.CreateAsync(
            new WhitelistStorageElement
            {
                TelegramUserId = userId,
                Id = Guid.NewGuid(),
            }
        );
    }

    private readonly ISqlRepository<WhitelistStorageElement> sqlRepository;
}