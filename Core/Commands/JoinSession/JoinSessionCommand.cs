using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Sessions.Models;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;

namespace Core.Commands.JoinSession;

public class JoinSessionCommand : CommandBase, ICommandWithoutSession, IInitiateSpotifyAuthCommand, IJoinSessionCommand
{
    public JoinSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<JoinSessionCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    protected override async Task ExecuteAsync()
    {
        var isCorrectSessionIdFormat = Guid.TryParse(Message, out var sessionIdToJoin);
        if (!isCorrectSessionIdFormat)
        {
            await SendResponseAsync(UserId, "Некорректный формат кода комнаты");
            return;
        }

        try
        {
            await SessionsService.JoinAsync(
                sessionIdToJoin, new SessionParticipant
                {
                    UserId = UserId,
                    UserName = UserName,
                }
            );
            var session = (await SessionsService.TryReadAsync(sessionIdToJoin))!;
            await NotifyAllAsync(
                session, $"{UserName} присоединяется\n"
                         + $"В этой комнате {session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
            );
        }
        catch (SessionNotFoundException)
        {
            await SendResponseAsync(UserId, $"Комната с кодом `{sessionIdToJoin}` не найдена", ParseMode.MarkdownV2);
        }
    }
}