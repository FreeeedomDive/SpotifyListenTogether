using Core.Commands.Base;
using Core.Commands.CreateSession;
using Core.Commands.Dev;
using Core.Commands.ForceAuth;
using Core.Commands.ForceSync;
using Core.Commands.GroupAddToQueue;
using Core.Commands.JoinSession;
using Core.Commands.LeaveSession;
using Core.Commands.NextTrack;
using Core.Commands.Pause;
using Core.Commands.PlayMusic;
using Core.Commands.Recognize;
using Core.Commands.SessionInfo;
using Core.Commands.Start;
using Core.Commands.StatsByArtists;
using Core.Commands.Unpause;
using Core.Commands.Whitelist;

namespace Core.Commands.Factory;

public class CommandsFactory : ICommandsFactory
{
    public CommandsFactory(
        IStartCommand startCommand,
        IWhitelistCommand whitelistCommand,
        ICreateSessionCommand createSessionCommand,
        ILeaveSessionCommand leaveSessionCommand,
        IJoinSessionCommand joinSessionCommand,
        IForceSyncCommand forceSyncCommand,
        IPauseCommand pauseCommand,
        IUnpauseCommand unpauseCommand,
        INextTrackCommand nextTrackCommand,
        IGroupAddSongsToQueueCommand groupAddSongsToQueueCommand,
        IForceAuthCommand forceAuthCommand,
        ISessionInfoCommand sessionInfoCommand,
        IPlaylistStatsByArtistCommand playlistStatsByArtistCommand,
        IPlayMusicCommand playMusicCommand,
        IMigrationCommand migrationCommand
    )
    {
        commandBuilders = new Dictionary<CommandType, Func<ICommandBase>>
        {
            { CommandType.Start, () => startCommand },
            { CommandType.Whitelist, () => whitelistCommand },
            { CommandType.CreateSession, () => createSessionCommand },
            { CommandType.LeaveSession, () => leaveSessionCommand },
            { CommandType.JoinSession, () => joinSessionCommand },
            { CommandType.ForceSync, () => forceSyncCommand },
            { CommandType.Pause, () => pauseCommand },
            { CommandType.Unpause, () => unpauseCommand },
            { CommandType.NextTrack, () => nextTrackCommand },
            { CommandType.GroupAddToQueue, () => groupAddSongsToQueueCommand },
            { CommandType.ForceAuth, () => forceAuthCommand },
            { CommandType.SessionInfo, () => sessionInfoCommand },
            { CommandType.StatsByArtists, () => playlistStatsByArtistCommand },
            { CommandType.PlayMusic, () => playMusicCommand },
            { CommandType.Migration, () => migrationCommand },
        };
    }

    public ICommandBase Build(CommandType commandType)
    {
        if (!commandBuilders.TryGetValue(commandType, out var commandBuilder))
        {
            throw new NotSupportedException($"Command {commandType} is not supported");
        }

        return commandBuilder();
    }

    private readonly Dictionary<CommandType, Func<ICommandBase>> commandBuilders;
}