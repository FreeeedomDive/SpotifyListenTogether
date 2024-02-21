using Core.Sessions.Models;

namespace Core.Sessions.Storage;

public interface ISessionsRepository
{
    Task<Session[]> ReadAllAsync();
    Task<Session?> TryReadAsync(Guid id);
    Task CreateAsync(Session session);
    Task UpdateAsync(Session session);
    Task DeleteAsync(Guid id);
}