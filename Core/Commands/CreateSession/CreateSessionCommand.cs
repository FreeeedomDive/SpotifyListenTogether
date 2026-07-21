using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Auth.Storage;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using SpotifyHelpers.Api.Client;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.CreateSession;

public class CreateSessionCommand : CommandBase, ICreateSessionCommand, ICommandWithoutSession, IInitiateSpotifyAuthCommand
{
    public CreateSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyProfilesService spotifyProfilesService,
        ISpotifyHelpersApiClient spotifyHelpersApiClient,
        IWhitelistService whitelistService,
        ILogger<CreateSessionCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyProfilesService, spotifyHelpersApiClient, whitelistService, logger)
    {
    }

    protected override async Task ExecuteAsync()
    {
        var newSessionId = await SessionsService.CreateAsync(
            new SessionParticipant
            {
                UserId = UserId,
                UserName = UserName,
            }
        );
        await SendResponseAsync(UserId, $"Создана комната `{newSessionId}`", ParseMode.MarkdownV2);
    }
}
