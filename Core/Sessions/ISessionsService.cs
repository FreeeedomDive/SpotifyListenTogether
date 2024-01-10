namespace Core.Sessions;

public interface ISessionsService
{
    Session? TryRead(Guid sessionId);
    Guid Create(long authorId, string username);
    Guid? Find(long userId);
    void Join(Guid sessionId, long userId);
    void Leave(Guid sessionId, long userId);
    void Destroy(Guid sessionId);
}