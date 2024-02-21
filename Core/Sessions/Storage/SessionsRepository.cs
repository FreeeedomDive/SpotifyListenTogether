using Core.Sessions.Models;
using Newtonsoft.Json;
using SqlRepositoryBase.Core.Repository;

namespace Core.Sessions.Storage;

public class SessionsRepository : ISessionsRepository
{
    public SessionsRepository(ISqlRepository<SessionStorageElement> sqlRepository)
    {
        this.sqlRepository = sqlRepository;
    }

    public async Task<Session[]> ReadAllAsync()
    {
        var result = await sqlRepository.ReadAllAsync();
        return result.Select(x => JsonConvert.DeserializeObject<Session>(x.SerializedSession)!).ToArray();
    }

    public async Task<Session?> TryReadAsync(Guid id)
    {
        var result = await sqlRepository.TryReadAsync(id);
        return result is null ? null : JsonConvert.DeserializeObject<Session>(result.SerializedSession);
    }

    public async Task CreateAsync(Session session)
    {
        var storageElement = new SessionStorageElement
        {
            Id = session.Id,
            SerializedSession = JsonConvert.SerializeObject(session, Formatting.Indented),
        };
        await sqlRepository.CreateAsync(storageElement);
    }

    public async Task UpdateAsync(Session session)
    {
        if (await TryReadAsync(session.Id) is null)
        {
            return;
        }
        await sqlRepository.UpdateAsync(session.Id, x => x.SerializedSession = JsonConvert.SerializeObject(session, Formatting.Indented));
    }

    public async Task DeleteAsync(Guid id)
    {
        await sqlRepository.DeleteAsync(id);
    }

    private readonly ISqlRepository<SessionStorageElement> sqlRepository;
}