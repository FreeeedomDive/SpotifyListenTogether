namespace Core.Sessions;

public interface ISessionsService
{
    Session? TryRead(Guid sessionId);
    Guid Create(SessionParticipant sessionParticipant);
    Guid? Find(long userId);
    void Join(Guid sessionId, SessionParticipant participant);
    void Leave(Guid sessionId, long userId);
    void Destroy(Guid sessionId);
}