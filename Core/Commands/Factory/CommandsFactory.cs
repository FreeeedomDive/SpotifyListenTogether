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
            { CommandType.Start, () => new StartCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.Whitelist, () => new WhitelistCommand(whitelistService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.CreateSession, () => new CreateSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.LeaveSession, () => new LeaveSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.JoinSession, () => new JoinSessionCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.ForceSync, () => new ForceSyncCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.Pause, () => new PauseCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.Unpause, () => new UnpauseCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.NextTrack, () => new NextTrackCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            {
                CommandType.GroupAddToQueue,
                () => new GroupAddSongsToQueueCommand(spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
            },
            { CommandType.ForceAuth, () => new ForceAuthCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            { CommandType.SessionInfo, () => new SessionInfoCommand(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient) },
            {
                CommandType.StatsByArtists,
                () => new PlaylistStatsByArtistCommand(spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
            },
            {
                CommandType.PlayMusic,
                () => new PlayMusicCommand(spotifyLinksRecognizeService, telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
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