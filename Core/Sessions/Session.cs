namespace Core.Sessions;

public class Session
{
    public Guid Id { get; set; }
    public long AuthorId { get; set; }
    public DateTime? LastActivity { get; set; }
    public List<SessionParticipant> Participants { get; set; }
}