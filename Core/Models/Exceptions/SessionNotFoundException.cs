namespace Core.Models.Exceptions;

public class SessionNotFoundException : Exception
{
    public SessionNotFoundException(Guid sessionId) : base($"Session with id {sessionId} not found")
    {
    }
}