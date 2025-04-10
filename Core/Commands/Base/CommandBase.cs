using System.Diagnostics;
using Core.Commands.Base.Interfaces;
using Core.Commands.ForceAuth;
using Core.Commands.Whitelist;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.Base;

public abstract class CommandBase : ICommandBase
{
    protected CommandBase(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger logger
    )
    {
        this.whitelistService = whitelistService;
        TelegramBotClient = telegramBotClient;
        SessionsService = sessionsService;
        SpotifyClientStorage = spotifyClientStorage;
        SpotifyClientFactory = spotifyClientFactory;
        Logger = logger;
    }

    public async Task ExecuteAsync(Message message)
    {
        UserId = message.Chat.Id;
        UserName = message.Chat.Username ?? $"{message.Chat.FirstName} {message.Chat.LastName}";
        Message = message.Text ?? string.Empty;
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var isWhitelisted = await whitelistService.IsUserWhitelistedAsync(UserId);
            if (!isWhitelisted && this is not WhitelistCommand)
            {
                Logger.LogWarning("User {UserName} ({UserId}) tried to use {CommandName}, but not whitelisted", UserName, UserId, CommandName);
                return;
            }

            // ReSharper disable once SuspiciousTypeConversion.Global - this is added for future validations
            if (this is ICommandWithoutSession && this is ICommandWithSession)
            {
                throw new NotSupportedException($"Command {CommandName} can't be with and without session simultaneously");
            }

            var session = await SessionsService.FindAsync(UserId);
            if (this is ICommandWithoutSession && session is not null)
            {
                await SendResponseAsync(UserId, $"Сначала нужно выйти из комнаты `{session.Id}`", ParseMode.MarkdownV2);
                return;
            }

            if (this is ICommandWithSession commandWithSession)
            {
                if (session is null)
                {
                    await SendResponseAsync(UserId, "Сначала нужно войти в комнату для совместного прослушивания");
                    return;
                }

                commandWithSession.Session = session;
            }

            if (this is ICommandWithSpotifyAuth commandWithSpotifyAuth)
            {
                var spotifyClient = await SpotifyClientFactory.GetAsync(UserId);
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
                    session!
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
                    }, Logger
                );
            }

            await ExecuteAsync();

            if (this is IInitiateSpotifyAuthCommand)
            {
#pragma warning disable CS4014
                Task.Run(StartSpotifyAuthAsync);
#pragma warning restore CS4014
            }

            if (session is not null)
            {
                await SessionsService.UpdateAsync(session);
            }
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Unexpected error in command {CommandName}", CommandName);
            await SendResponseAsync(UserId, $"Unexpected error in command {CommandName}");
        }
        finally
        {
            Logger.LogInformation("{UserName} used command {CommandName}, elapsed {Milliseconds}ms", UserName, CommandName, stopwatch.ElapsedMilliseconds);
        }
    }

    private async Task StartSpotifyAuthAsync()
    {
        try
        {
            var forceReAuth = this is IForceAuthCommand;
            var spotifyClient = await SpotifyClientFactory.CreateOrGetAsync(UserId, forceReAuth);
            if (spotifyClient is null)
            {
                await SendResponseAsync(UserId, "Истекло время для авторизации");
                return;
            }

            var spotifyUser = await spotifyClient.UserProfile.Current();
            await SendResponseAsync(UserId, $"Успешная авторизация в Spotify как {spotifyUser.DisplayName}");
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Exception in auth");
        }
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
    protected ILogger Logger { get; }
    private readonly IWhitelistService whitelistService;
}