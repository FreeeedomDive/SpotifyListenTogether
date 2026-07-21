using System.Diagnostics;
using Core.Commands.Base.Interfaces;
using Core.Commands.ForceAuth;
using Core.Commands.Whitelist;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.Base;

public abstract class CommandBase : ICommandBase
{
    protected CommandBase(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger logger
    )
    {
        this.whitelistService = whitelistService;
        TelegramBotClient = telegramBotClient;
        SessionsService = sessionsService;
        this.spotifyProfilesService = spotifyProfilesService;
        SpotifyHelpersApiClient = spotifyHelpersApiClient;
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
                var profileId = await spotifyProfilesService.TryGetAuthorizedProfileIdAsync(UserId);
                if (profileId is null)
                {
                    await SendResponseAsync(UserId, "Сначала нужно пройти авторизацию в Spotify");
                    return;
                }

                commandWithSpotifyAuth.SpotifyProfileId = profileId.Value;
            }

            if (this is ICommandForAllParticipants commandForAllParticipants)
            {
                var profileIds = await spotifyProfilesService.ReadAuthorizedProfileIdsAsync(
                    session!.Participants.Select(x => x.UserId));
                commandForAllParticipants.UserIdToSpotifyProfile = session.Participants
                    .Where(x => profileIds.ContainsKey(x.UserId))
                    .ToDictionary(
                        x => x.UserId,
                        x => (Participant: x, ProfileId: profileIds[x.UserId]));
            }

            if (this is ICommandWithAliveDeviceValidation commandWithAliveDeviceValidation)
            {
                await commandWithAliveDeviceValidation.ApplyToAllParticipants(
                    async (profileId, participant) =>
                    {
                        if (participant.DeviceId is null)
                        {
                            return;
                        }

                        var devices = await SpotifyHelpersApiClient.PlayerV2.GetDevicesAsync(profileId);
                        if (devices.Items.All(x => x.Id != participant.DeviceId))
                        {
                            participant.DeviceId = null;
                        }
                    }, Logger
                );
            }

            await ExecuteAsync();

            if (this is IInitiateSpotifyAuthCommand)
            {
                _ = StartSpotifyAuthAsync();
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
            var profileId = await spotifyProfilesService.GetOrCreateProfileIdAsync(UserId);
            if (forceReAuth || !await spotifyProfilesService.IsAuthorizedAsync(profileId))
            {
                var authorization = await SpotifyHelpersApiClient.Auth.StartSpotifyAuthorizationAsync(profileId);
                await SendResponseAsync(
                    UserId,
                    $"Теперь нужно авторизоваться в Spotify по этой ссылке: {authorization.AuthorizationUrl}\n(ссылка активна минуту)");

                var result = await SpotifyHelpersApiClient.Auth.WaitForSpotifyAuthorizationAsync(
                    profileId,
                    authorization.State);
                if (!result.IsAuthorized)
                {
                    await SendResponseAsync(UserId, "Истекло время для авторизации");
                    return;
                }
            }

            var spotifyUser = await SpotifyHelpersApiClient.PlayerV2.GetCurrentUserAsync(profileId);
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
    protected ISpotifyHelpersApiClient SpotifyHelpersApiClient { get; }
    protected ILogger Logger { get; }
    private readonly IWhitelistService whitelistService;
    private readonly ISpotifyProfilesService spotifyProfilesService;
}
