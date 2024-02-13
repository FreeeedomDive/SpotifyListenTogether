using Core.Commands.Base;
using Core.Sessions;
using Core.Spotify.Client;
using Telegram.Bot;
using TelemetryApp.Api.Client.Log;

namespace Core.Commands;

public class StartCommand : CommandBase
{
    public StartCommand(
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
        await SendResponseAsync(
            UserId, "Привет!\n"
                    + "Этот бот позволяет слушать музыку в комнатах для совместного прослушивания. "
                    + "Несколько человек могут одновременно слушать музыку, ставить треки в очередь, включать альбомы и плейлисты.\n"
                    + "Важное ограничение 1! Перед тем, как начать совместное прослушивание, нужно \"разбудить\" клиент спотифая на том устройстве, где ты будешь слушать музыку. "
                    + "Это нужно для того, чтобы API спотифая увидел это устройство и посылал на него запросы.\n"
                    + "Важное ограничение 2! Авторизация в спотифай происходит через браузер, поэтому при первоначальной авторизации нужно иметь включенный VPN, "
                    + "так как на запросы из России спотифай отдает 403 ошибку. Впоследствии можно будет авторизовываться без VPN.\n"
                    + "Для начала создай свою комнату и передай код комнаты друзьям, либо введи код уже созданной комнаты."
        );
    }
}