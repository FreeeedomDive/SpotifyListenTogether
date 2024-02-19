using Core.Sessions.Storage;

namespace Core.Sessions;

public class SessionsService : ISessionsService
{
    public SessionsService(ISessionsRepository sessionsRepository)
    {
        this.sessionsRepository = sessionsRepository;
    }

    public async Task InitializeAsync()
    {
        var sessions = await sessionsRepository.ReadAllAsync();
        foreach (var session in sessions)
        {
            foreach (var participant in session.Participants)
            {
                sessionsByUser.Add(participant.UserId, session.Id);
            }
        }
    }

    public async Task<Session?> TryReadAsync(Guid sessionId)
    {
        return await sessionsRepository.TryReadAsync(sessionId);
    }

    public async Task<Guid> CreateAsync(SessionParticipant sessionParticipant)
    {
        var authorId = sessionParticipant.UserId;
        var newSession = new Session
        {
            Id = Guid.NewGuid(),
            AuthorId = authorId,
            Participants = new List<SessionParticipant> { sessionParticipant },
        };
        await sessionsRepository.CreateAsync(newSession);
        sessionsByUser.Add(authorId, newSession.Id);
        return newSession.Id;
    }

    public async Task<Session?> FindAsync(long userId)
    {
        var sessionId = sessionsByUser.TryGetValue(userId, out var x) ? x : (Guid?)null;
        if (sessionId is null)
        {
            return null;
        }

        var session = await sessionsRepository.TryReadAsync(sessionId.Value);
        if (session is not null)
        {
            return session;
        }

        sessionsByUser.Remove(userId);
        return null;
    }

    public async Task UpdateAsync(Session session)
    {
        await sessionsRepository.UpdateAsync(session);
    }

    public async Task JoinAsync(Guid sessionId, SessionParticipant participant)
    {
        var userId = participant.UserId;
        var currentUserSession = await FindAsync(userId);
        if (currentUserSession is not null)
        {
            currentUserSession.Participants.RemoveAll(x => x.UserId == userId);
            await sessionsRepository.UpdateAsync(currentUserSession);
        }

        var session = await TryReadAsync(sessionId);
        if (session is null)
        {
            throw new SessionNotFoundException(sessionId);
        }

        session.Participants.Add(participant);
        await sessionsRepository.UpdateAsync(session);
        sessionsByUser.Add(userId, sessionId);
    }

    public async Task LeaveAsync(Guid sessionId, long userId)
    {
        var session = await sessionsRepository.TryReadAsync(sessionId);
        if (session is not null)
        {
            session.Participants.RemoveAll(x => x.UserId == userId);
            await sessionsRepository.UpdateAsync(session);
        }

        if (sessionsByUser.TryGetValue(userId, out var userSessionId) && userSessionId == sessionId)
        {
            sessionsByUser.Remove(userId);
        }
    }

    public async Task DestroyAsync(Guid sessionId)
    {
        var session = await sessionsRepository.TryReadAsync(sessionId);
        if (session is null)
        {
            return;
        }

        foreach (var participant in session.Participants)
        {
            sessionsByUser.Remove(participant.UserId);
        }

        await sessionsRepository.DeleteAsync(sessionId);
    }

    private readonly Dictionary<long, Guid> sessionsByUser = new();
    private readonly ISessionsRepository sessionsRepository;
}