using Core.Commands.Factory;
using Core.Commands.Recognize;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Core.TelegramWorker;

public class TelegramBotWorker : ITelegramBotWorker
{
    public TelegramBotWorker(
        ITelegramBotClient telegramBotClient,
        ILogger<TelegramBotWorker> logger,
        ICommandsRecognizer commandsRecognizer,
        ICommandsFactory commandsFactory
    )
    {
        this.telegramBotClient = telegramBotClient;
        this.logger = logger;
        this.commandsRecognizer = commandsRecognizer;
        this.commandsFactory = commandsFactory;
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

        await Task.Delay(-1);
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Telegram polling error");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: not null } message)
        {
            return;
        }

        var userId = message.Chat.Id;
        try
        {
            var commandType = commandsRecognizer.ParseCommand(message);
            if (!commandType.HasValue)
            {
                await telegramBotClient.SendTextMessageAsync(userId, "Команда не распознана", cancellationToken: cancellationToken);
                return;
            }

            var command = commandsFactory.Build(commandType.Value);
            await command.ExecuteAsync(message);
        }
        catch (Exception exception)
        {
            await telegramBotClient.SendTextMessageAsync(userId, exception.Message, cancellationToken: cancellationToken);
        }
    }

    private readonly ICommandsFactory commandsFactory;
    private readonly ICommandsRecognizer commandsRecognizer;
    private readonly ILogger<TelegramBotWorker> logger;
    private readonly ITelegramBotClient telegramBotClient;
}