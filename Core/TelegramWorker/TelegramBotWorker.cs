using Core.Extensions;
using Core.Models.Exceptions;
using Core.Sessions;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Core.TelegramWorker;

public class TelegramBotWorker : ITelegramBotWorker
{
    public TelegramBotWorker(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService
    )
    {
        this.telegramBotClient = telegramBotClient;
        this.sessionsService = sessionsService;
    }

    public async Task StartAsync()
    {
        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
        };

        telegramBotClient.StartReceiving(
            HandleUpdateAsync,
            HandlePollingErrorAsync,
            receiverOptions
        );

        Console.WriteLine("Starting bot...");
        await Task.Delay(-1);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var currentSessionId = sessionsService.Find(chatId);
        switch (messageText)
        {
            case "/create":
                await HandleCreateSessionAsync(chatId, currentSessionId);
                break;
            case "/leave":
                await HandleLeaveSessionAsync(chatId, currentSessionId);
                break;
            default:
                await HandleMessageAsync(chatId, currentSessionId, messageText);
                break;
        }
    }

    private async Task HandleCreateSessionAsync(long chatId, Guid? currentSessionId)
    {
        if (currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, $"Сейчас ты находишься в комнате ```{currentSessionId}```, новую комнату создать нельзя");
            return;
        }

        var newSessionId = sessionsService.Create(chatId);
        await SendResponseAsync(chatId, $"Создана комната ```{newSessionId}```");
    }

    private async Task HandleLeaveSessionAsync(long chatId, Guid? currentSessionId)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Нет текущей активной комнаты");
            return;
        }

        sessionsService.Leave(currentSessionId.Value, chatId);
        await SendResponseAsync(chatId, $"Ты покинул комнату ```{currentSessionId}```");
    }

    private async Task HandleMessageAsync(long chatId, Guid? currentSessionId, string messageText)
    {
        if (!currentSessionId.HasValue)
        {
            await HandleJoinSessionAsync(chatId, messageText);
            return;
        }

        await HandleAddMusicInSessionAsync(chatId, currentSessionId, messageText);
    }

    private async Task HandleJoinSessionAsync(long chatId, string messageText)
    {
        var isCorrectSessionIdFormat = Guid.TryParse(messageText, out var sessionIdToJoin);
        if (!isCorrectSessionIdFormat)
        {
            await SendResponseAsync(chatId, "Некорректный формат кода комнаты");
            return;
        }

        try
        {
            sessionsService.Join(sessionIdToJoin, chatId);
            var session = sessionsService.TryRead(sessionIdToJoin)!;
            await SendResponseAsync(
                chatId,
                $"Успешный вход в комнату ```{sessionIdToJoin}```\n"
                + $"В этой комнате {session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
            );
        }
        catch (SessionNotFoundException)
        {
            await SendResponseAsync(chatId, $"Комната с кодом ```{sessionIdToJoin}``` не найдена");
        }
    }

    private async Task HandleAddMusicInSessionAsync(long chatId, Guid? currentSessionId, string messageText)
    {
        await SendResponseAsync(chatId, $"Текущая комната: ```{currentSessionId}```");
    }

    private async Task SendResponseAsync(long chatId, string message)
    {
        await telegramBotClient.SendTextMessageAsync(chatId, message, parseMode: ParseMode.MarkdownV2);
    }

    private readonly ISessionsService sessionsService;
    private readonly ITelegramBotClient telegramBotClient;
}