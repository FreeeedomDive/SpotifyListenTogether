using Core.Commands.Base;
using Core.Commands.Recognize;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Spotify.Links;
using Core.Whitelist;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.Factory;

public class CommandsFactory : ICommandsFactory
{
    public CommandsFactory(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientFactory spotifyClientFactory,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        IWhitelistService whitelistService,
        ILoggerClient loggerClient
    )
    {
        commandBuilders = new Dictionary<CommandType, Func<CommandBase>>
        {
            { CommandType.Start, () => new StartCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            { CommandType.Whitelist, () => new WhitelistCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            {
                CommandType.CreateSession,
                () => new CreateSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
            },
            {
                CommandType.LeaveSession,
                () => new LeaveSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
            },
            {
                CommandType.JoinSession,
                () => new JoinSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
            },
            { CommandType.ForceSync, () => new ForceSyncCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            { CommandType.Pause, () => new PauseCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            { CommandType.Unpause, () => new UnpauseCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            { CommandType.NextTrack, () => new NextTrackCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            {
                CommandType.GroupAddToQueue,
                () => new GroupAddSongsToQueueCommand(
                    spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient
                )
            },
            { CommandType.ForceAuth, () => new ForceAuthCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient) },
            {
                CommandType.SessionInfo,
                () => new SessionInfoCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient)
            },
            {
                CommandType.StatsByArtists,
                () => new PlaylistStatsByArtistCommand(
                    spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient
                )
            },
            {
                CommandType.PlayMusic,
                () => new PlayMusicCommand(
                    spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, loggerClient
                )
            },
        };
    }

    public CommandBase Build(CommandType commandType)
    {
        if (!commandBuilders.TryGetValue(commandType, out var commandBuilder))
        {
            throw new NotSupportedException($"Command {commandType} is not supported");
        }

        return commandBuilder();
    }

    private readonly Dictionary<CommandType, Func<CommandBase>> commandBuilders;
}