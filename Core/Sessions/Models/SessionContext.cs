namespace Core.Sessions.Models;

public class SessionContext
{
    public string? ContextUri { get; set; }
    public string? TrackUri { get; set; }
    public int? PositionMs { get; set; }
}