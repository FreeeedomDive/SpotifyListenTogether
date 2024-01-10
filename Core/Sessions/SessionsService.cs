namespace Core.Sessions;

public class SessionsService : ISessionsService
{
    public Session? TryRead(Guid sessionId)
    {
        return sessions.TryGetValue(sessionId, out var session) ? session : null;
    }

    public Guid Create(long authorId, string username)
    {
        var newSession = new Session
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Participants = new List<SessionParticipant>
            {
                new()
                {
                    UserId = authorId,
                    DeviceId = null,
                    UserName = username,
                },
            },
        };
        sessions.Add(newSession.Id, newSession);
        sessionsByUser.Add(authorId, newSession.Id);
        return newSession.Id;
    }

    public Guid? Find(long userId)
    {
        return sessionsByUser.TryGetValue(userId, out var sessionId) ? sessionId : null;
    }

    public void Join(Guid sessionId, long userId)
    {
        var currentUserSession = Find(userId);
        if (currentUserSession.HasValue)
        {
            var oldSession = sessions[currentUserSession.Value];
            oldSession.Participants.RemoveAll(x => x.UserId == userId);
        }

        var session = TryRead(sessionId);
        if (session is null)
        {
            throw new SessionNotFoundException(sessionId);
        }

        session.Participants.Add(new SessionParticipant
        {
            UserId = userId,
            DeviceId = null,
        });
        sessionsByUser.Add(userId, sessionId);
    }

    public void Leave(Guid sessionId, long userId)
    {
        if (sessions.TryGetValue(sessionId, out var session))
        {
            session.Participants.RemoveAll(x => x.UserId == userId);
        }

        if (sessionsByUser.TryGetValue(userId, out var userSessionId) && userSessionId == sessionId)
        {
            sessionsByUser.Remove(userId);
        }
    }

    public void Destroy(Guid sessionId)
    {
        var sessionFound = sessions.TryGetValue(sessionId, out var session);
        if (!sessionFound || session is null)
        {
            return;
        }

        foreach (var participant in session.Participants)
        {
            sessionsByUser.Remove(participant.UserId);
        }

        sessions.Remove(sessionId);
    }

    private readonly Dictionary<Guid, Session> sessions = new();
    private readonly Dictionary<long, Guid> sessionsByUser = new();
}