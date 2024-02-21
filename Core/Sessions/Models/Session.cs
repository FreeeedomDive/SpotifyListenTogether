namespace Core.Sessions.Models;

public class Session
{
    public Guid Id { get; set; }
    public long AuthorId { get; set; }
    public List<SessionParticipant> Participants { get; set; }
}