using Core.Sessions.Models;

namespace Core.Extensions;

public static class SessionExtensions
{
    public static void Leave(this Session session, long userId)
    {
        session.Participants.RemoveAll(x => x.UserId == userId);
    }
}