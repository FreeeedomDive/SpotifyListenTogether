namespace Core.Whitelist;

public interface IWhitelistService
{
    Task<bool> IsUserWhitelistedAsync(long userId);
    Task AddToWhitelistAsync(long userId);
}