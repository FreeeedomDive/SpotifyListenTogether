namespace Core.Commands.Recognize;

public enum CommandType
{
    // session controls
    CreateSession,
    JoinSession,
    LeaveSession,

    // spotify controls
    ForceSync,
    Pause,
    Unpause,
    NextTrack,
    PlayMusic,
    SessionInfo,

    // other commands
    Start,
    ForceAuth,

    // stats
    StatsByArtist,
}