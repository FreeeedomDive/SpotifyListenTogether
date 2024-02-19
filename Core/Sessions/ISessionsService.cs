namespace Core.Sessions;

public interface ISessionsService
{
    Task InitializeAsync();
    Task<Session?> TryReadAsync(Guid sessionId);
    Task<Guid> CreateAsync(SessionParticipant sessionParticipant);
    Task<Session?> FindAsync(long userId);
    Task UpdateAsync(Session session);
    Task JoinAsync(Guid sessionId, SessionParticipant participant);
    Task LeaveAsync(Guid sessionId, long userId);
    Task DestroyAsync(Guid sessionId);
}