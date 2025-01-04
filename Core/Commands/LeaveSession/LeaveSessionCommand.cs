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

namespace Core.Commands.LeaveSession;

public class LeaveSessionCommand : CommandBase, ICommandWithSession, ILeaveSessionCommand
{
    public LeaveSessionCommand(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<LeaveSessionCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
    }

    public Session Session { get; set; } = null!;

    protected override async Task ExecuteAsync()
    {
        Session.Leave(UserId);
        await SessionsService.LeaveAsync(Session.Id, UserId);
        await NotifyAllAsync(
            Session, $"{UserName} выходит из комнаты\n"
                     + $"В этой комнате {Session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
        );
        var shouldDestroySession = !Session.Participants.Any();
        if (shouldDestroySession)
        {
            await SessionsService.DestroyAsync(Session.Id);
        }

        await SendResponseAsync(
            UserId,
            $"Ты покинул комнату `{Session.Id}`" + (shouldDestroySession ? "\nТы был последним слушателем в этой комнате, она будет удалена" : string.Empty),
            ParseMode.MarkdownV2
        );
    }
}