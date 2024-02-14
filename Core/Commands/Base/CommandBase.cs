using System.Diagnostics;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands.Base;

public abstract class CommandBase
{
    public CommandBase(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    )
    {
        TelegramBotClient = telegramBotClient;
        SessionsService = sessionsService;
        SpotifyClientStorage = spotifyClientStorage;
        SpotifyClientFactory = spotifyClientFactory;
        LoggerClient = loggerClient;
    }

    public async Task ExecuteAsync(Message message)
    {
        UserId = message.Chat.Id;
        UserName = message.Chat.Username ?? $"{message.Chat.FirstName} {message.Chat.LastName}";
        Message = message.Text ?? string.Empty;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            // ReSharper disable once SuspiciousTypeConversion.Global - this is added for future validations
            if (this is ICommandWithoutSession && this is ICommandWithSession)
            {
                throw new NotSupportedException($"Command {CommandName} can't be with and without session simultaneously");
            }

            var sessionId = SessionsService.Find(UserId);
            if (this is ICommandWithoutSession && sessionId.HasValue)
            {
                await SendResponseAsync(UserId, $"Сначала нужно выйти из комнаты `{sessionId.Value}`", ParseMode.MarkdownV2);
                return;
            }

            if (this is ICommandWithSession commandWithSession)
            {
                if (!sessionId.HasValue)
                {
                    await SendResponseAsync(UserId, "Сначала нужно войти в комнату для совместного прослушивания");
                    return;
                }

                var session = SessionsService.TryRead(sessionId.Value)!;
                commandWithSession.Session = session;
            }

            if (this is ICommandWithSpotifyAuth commandWithSpotifyAuth)
            {
                var spotifyClient = SpotifyClientStorage.TryRead(UserId);
                if (spotifyClient is null)
                {
                    await SendResponseAsync(UserId, "Сначала нужно пройти авторизацию в Spotify");
                    return;
                }

                commandWithSpotifyAuth.SpotifyClient = spotifyClient;
            }

            if (this is ICommandForAllParticipants commandForAllParticipants)
            {
                commandForAllParticipants.UserIdToSpotifyClient =
                    commandForAllParticipants
                        .Session
                        .Participants
                        .Select(participant => (Participant: participant, SpotifyClient: SpotifyClientStorage.TryRead(participant.UserId)))
                        .Where(pair => pair.SpotifyClient is not null)
                        .ToDictionary(pair => pair.Participant.UserId, pair => (pair.Participant, pair.SpotifyClient!));
            }

            if (this is ICommandWithAliveDeviceValidation commandWithAliveDeviceValidation)
            {
                await commandWithAliveDeviceValidation.ApplyToAllParticipants(
                    async (spotifyClient, participant) =>
                    {
                        if (participant.DeviceId is null)
                        {
                            return;
                        }

                        var devices = await spotifyClient.Player.GetAvailableDevices();
                        if (devices.Devices.All(x => x.Id != participant.DeviceId))
                        {
                            participant.DeviceId = null;
                        }
                    }, LoggerClient
                );
            }

            await ExecuteAsync();

            if (this is IInitiateSpotifyAuthCommand)
            {
#pragma warning disable CS4014
                Task.Run(StartSpotifyAuthAsync);
#pragma warning restore CS4014
            }
        }
        catch (Exception exception)
        {
            await LoggerClient.ErrorAsync(exception, "Unexpected error in command {CommandName}", CommandName);
            await SendResponseAsync(UserId, $"Unexpected error in command {CommandName}");
        }
        finally
        {
            await LoggerClient.InfoAsync("{UserName} used command {CommandName}, elapsed {Milliseconds}ms", UserName, CommandName, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task StartSpotifyAuthAsync()
    {
        var forceReAuth = this is ForceAuthCommand;
        var spotifyClient = await SpotifyClientFactory.CreateOrGetAsync(UserId, forceReAuth);
        if (spotifyClient is null)
        {
            await SendResponseAsync(UserId, "Истекло время для авторизации");
            return;
        }

        var spotifyUser = await spotifyClient.UserProfile.Current();
        await SendResponseAsync(UserId, $"Успешная авторизация в Spotify как {spotifyUser.DisplayName}");
    }

    protected async Task SendResponseAsync(long chatId, string message, ParseMode? parseMode = null)
    {
        if (parseMode is null)
        {
            await TelegramBotClient.SendTextMessageAsync(chatId, message);
            return;
        }

        await TelegramBotClient.SendTextMessageAsync(chatId, message, parseMode: parseMode);
    }

    protected async Task NotifyAllAsync(Session session, string message, ParseMode? parseMode = null)
    {
        await Task.WhenAll(
            session.Participants.Select(
                participant => SendResponseAsync(participant.UserId, message, parseMode)
            )
        );
    }

    protected abstract Task ExecuteAsync();
    private string CommandName => GetType().Name;

    protected long UserId { get; private set; }
    protected string Message { get; private set; } = null!;
    protected string UserName { get; private set; } = null!;

    private ITelegramBotClient TelegramBotClient { get; }
    protected ISessionsService SessionsService { get; }
    private ISpotifyClientStorage SpotifyClientStorage { get; }
    private ISpotifyClientFactory SpotifyClientFactory { get; }
    protected ILoggerClient LoggerClient { get; }
}