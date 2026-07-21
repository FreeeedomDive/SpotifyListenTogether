using Core.Commands.Base;
using Core.Commands.CreateSession;
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

public class CommandsFactory(IServiceProvider serviceProvider) : ICommandsFactory
{
    public ICommandBase Build(CommandType commandType)
    {
        if (!commandTypes.TryGetValue(commandType, out var commandInterface))
        {
            throw new NotSupportedException($"Command {commandType} is not supported");
        }

        return serviceProvider.GetService(commandInterface) as ICommandBase
               ?? throw new InvalidOperationException($"Command {commandType} is not registered");
    }

    private readonly Dictionary<CommandType, Type> commandTypes = new()
    {
        { CommandType.Start, typeof(IStartCommand) },
        { CommandType.Whitelist, typeof(IWhitelistCommand) },
        { CommandType.CreateSession, typeof(ICreateSessionCommand) },
        { CommandType.LeaveSession, typeof(ILeaveSessionCommand) },
        { CommandType.JoinSession, typeof(IJoinSessionCommand) },
        { CommandType.ForceSync, typeof(IForceSyncCommand) },
        { CommandType.Pause, typeof(IPauseCommand) },
        { CommandType.Unpause, typeof(IUnpauseCommand) },
        { CommandType.NextTrack, typeof(INextTrackCommand) },
        { CommandType.GroupAddToQueue, typeof(IGroupAddSongsToQueueCommand) },
        { CommandType.ForceAuth, typeof(IForceAuthCommand) },
        { CommandType.SessionInfo, typeof(ISessionInfoCommand) },
        { CommandType.StatsByArtists, typeof(IPlaylistStatsByArtistCommand) },
        { CommandType.PlayMusic, typeof(IPlayMusicCommand) },
    };
}