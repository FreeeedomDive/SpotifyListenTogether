using Core.Commands.Factory;
using Core.Commands.Recognize;
using Core.Whitelist;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelemetryApp.Api.Client.Log;

namespace Core.TelegramWorker;

public class TelegramBotWorker : ITelegramBotWorker
{
    public TelegramBotWorker(
        ITelegramBotClient telegramBotClient,
        ILoggerClient loggerClient,
        ICommandsRecognizer commandsRecognizer,
        ICommandsFactory commandsFactory,
        IWhitelistService whitelistService
    )
    {
        this.telegramBotClient = telegramBotClient;
        this.loggerClient = loggerClient;
        this.commandsRecognizer = commandsRecognizer;
        this.commandsFactory = commandsFactory;
        this.whitelistService = whitelistService;
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

    private async Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        await loggerClient.ErrorAsync(exception, "Telegram polling error");
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
    private readonly ILoggerClient loggerClient;
    private readonly ITelegramBotClient telegramBotClient;
    private readonly IWhitelistService whitelistService;
}