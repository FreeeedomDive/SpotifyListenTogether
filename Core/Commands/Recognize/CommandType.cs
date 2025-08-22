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
    GroupAddToQueue,
    SessionInfo,

    // other commands
    Start,
    ForceAuth,
    Whitelist,
    Migration,

    // stats
    StatsByArtists,
}