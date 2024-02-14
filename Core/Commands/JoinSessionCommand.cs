using Core.Commands.Base;
using Core.Commands.Base.Interfaces;
using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class JoinSessionCommand : CommandBase, ICommandWithoutSession, IInitiateSpotifyAuthCommand
{
    public JoinSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        ILoggerClient loggerClient
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, loggerClient)
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
            SessionsService.Join(
                sessionIdToJoin, new SessionParticipant
                {
                    UserId = UserId,
                    UserName = UserName,
                }
            );
            var session = SessionsService.TryRead(sessionIdToJoin)!;
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