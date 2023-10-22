namespace Core.Sessions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(Guid sessionId) : base($"Session with id {sessionId} not found")
    {
    }
}