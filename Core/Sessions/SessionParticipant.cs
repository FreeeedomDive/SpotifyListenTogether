namespace Core.Sessions;

public class SessionParticipant
{
    public long UserId { get; set; }
    public string? DeviceId { get; set; }
    public string UserName { get; set; }
}