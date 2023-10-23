using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Spotify.Links;
using SpotifyAPI.Web;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Core.TelegramWorker;

public class TelegramBotWorker : ITelegramBotWorker
{
    public TelegramBotWorker(
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientFactory spotifyClientFactory,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService
    )
    {
        this.telegramBotClient = telegramBotClient;
        this.sessionsService = sessionsService;
        this.spotifyClientFactory = spotifyClientFactory;
        this.spotifyClientStorage = spotifyClientStorage;
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
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
        var username = $"{message.Chat.FirstName} {message.Chat.LastName}";
        try
        {
            switch (messageText)
            {
                case "/start":
                    await HandleStartAsync(chatId);
                    break;
                case "/create":
                    await HandleCreateSessionAsync(chatId, currentSessionId);
                    break;
                case "/leave":
                    await HandleLeaveSessionAsync(chatId, currentSessionId);
                    break;
                case "/forcesync":
                    await HandleForceSyncAsync(chatId, currentSessionId, username);
                    break;
                case "/pause":
                    await HandlePauseAsync(chatId, currentSessionId, username);
                    break;
                case "/unpause":
                    await HandleUnpauseAsync(chatId, currentSessionId, username);
                    break;
                case "/next":
                    await HandleNextTrackAsync(chatId, currentSessionId, username);
                    break;
                default:
                    await HandleMessageAsync(chatId, currentSessionId, messageText, username);
                    break;
            }
        }
        catch (Exception e)
        {
            await SendResponseAsync(chatId, e.Message);
        }
    }

    private async Task HandleStartAsync(long chatId)
    {
        await SendResponseAsync(
            chatId, "Привет!\n"
                    + "Этот бот позволяет слушать музыку в комнатах для совместного прослушивания. "
                    + "Несколько человек могут одновременно слушать музыку, ставить треки в очередь, включать альбомы и плейлисты.\n"
                    + "Важное ограничение 1! Перед тем, как начать совместное прослушивание, нужно \"разбудить\" клиент спотифая на том устройстве, где ты будешь слушать музыку. "
                    + "Это нужно для того, чтобы API спотифая увидел это устройство и посылал на него запросы.\n"
                    + "Важное ограничение 2! Авторизация в спотифай происходит через браузер, поэтому при первоначальной авторизации нужно иметь включенный VPN, "
                    + "так как на запросы из России спотифай отдает 403 ошибку. Впоследствии можно будет авторизовываться без VPN.\n"
                    + "Для начала создай свою комнату и передай код комнаты друзьям, либо введи код уже созданной комнаты."
        );
    }

    private async Task HandleCreateSessionAsync(long chatId, Guid? currentSessionId)
    {
        if (currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, $"Сейчас ты находишься в комнате ```{currentSessionId}```, новую комнату создать нельзя", ParseMode.MarkdownV2);
            return;
        }

        var newSessionId = sessionsService.Create(chatId);
        await SendResponseAsync(chatId, $"Создана комната ```{newSessionId}```", ParseMode.MarkdownV2);
        // start spotify auth in background to not block telegram messages handler 
        Task.Run(() => StartSpotifyAuthAsync(chatId));
    }

    private async Task HandleLeaveSessionAsync(long chatId, Guid? currentSessionId)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Нет текущей активной комнаты");
            return;
        }

        sessionsService.Leave(currentSessionId.Value, chatId);
        await SendResponseAsync(chatId, $"Ты покинул комнату ```{currentSessionId}```", ParseMode.MarkdownV2);
    }

    private async Task HandleMessageAsync(long chatId, Guid? currentSessionId, string messageText, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await HandleJoinSessionAsync(chatId, messageText);
            return;
        }

        await HandleAddMusicInSessionAsync(chatId, currentSessionId, messageText, username);
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
                + $"В этой комнате {session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}",
                ParseMode.MarkdownV2
            );
            // start spotify auth in background to not block telegram messages handler 
            Task.Run(() => StartSpotifyAuthAsync(chatId));
        }
        catch (SessionNotFoundException)
        {
            await SendResponseAsync(chatId, $"Комната с кодом ```{sessionIdToJoin}``` не найдена", ParseMode.MarkdownV2);
        }
    }

    private async Task HandleAddMusicInSessionAsync(long chatId, Guid? currentSessionId, string messageText, string username)
    {
        var spotifyClient = spotifyClientStorage.TryRead(chatId);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Сначала нужно пройти авторизацию в Spotify");
            return;
        }

        var spotifyLink = await spotifyLinksRecognizeService.TryRecognizeAsync(messageText);
        if (spotifyLink is null)
        {
            await SendResponseAsync(chatId, "Не смог распознать ссылку");
            return;
        }

        switch (spotifyLink.Type)
        {
            case SpotifyLinkType.Track:
                var track = await spotifyClient.Tracks.Get(spotifyLink.Id);
                await ApplyToAllParticipants(currentSessionId!.Value, client => client.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri)));
                await NotifyAllAsync(currentSessionId.Value, $"{username} добавляет в очередь {track.Artists.First().Name} - {track.Name}");
                break;
            case SpotifyLinkType.Artist:
                await SendResponseAsync(chatId, "Воспроизведение исполнителей не поддерживается, советуем найти плейлист с этим исполнителем и воспроизвести его.");
                break;
            case SpotifyLinkType.Album:
                var album = await spotifyClient.Albums.Get(spotifyLink.Id);
                await ApplyToAllParticipants(
                    currentSessionId!.Value, async client =>
                    {
                        await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                        await client.Player.ResumePlayback(
                            new PlayerResumePlaybackRequest
                            {
                                ContextUri = album.Uri,
                            }
                        );
                    }
                );
                await NotifyAllAsync(currentSessionId.Value, $"{username} начинает воспроизведение альбома {album.Name} исполнителя {album.Artists.First().Name}");
                break;
            case SpotifyLinkType.Playlist:
                var playlist = await spotifyClient.Playlists.Get(spotifyLink.Id);
                
                await ApplyToAllParticipants(
                    currentSessionId!.Value, async client =>
                    {
                        await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                        await client.Player.ResumePlayback(
                            new PlayerResumePlaybackRequest
                            {
                                ContextUri = playlist.Uri,
                            }
                        );
                    }
                );
                await NotifyAllAsync(currentSessionId.Value, $"{username} начинает воспроизведение плейлиста {playlist.Name}");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task HandleForceSyncAsync(long chatId, Guid? currentSessionId, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Сначала нужно войти в комнату для совместного прослушивания");
            return;
        }

        var spotifyClient = spotifyClientStorage.TryRead(chatId);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Сначала нужно пройти авторизацию в Spotify");
            return;
        }

        var clients = GetAllParticipantClients(currentSessionId.Value);
        var allCurrentProgress = await Task.WhenAll(
            clients.Select(
                async client => (await client.Player.GetCurrentPlayback()).ProgressMs
            )
        );
        var minProgress = allCurrentProgress.Min();
        await ApplyToAllParticipants(
            currentSessionId.Value, client => client.Player.ResumePlayback(
                new PlayerResumePlaybackRequest
                {
                    PositionMs = minProgress,
                }
            )
        );
        await NotifyAllAsync(currentSessionId.Value, $"{username} сбрасывает прогресс воспроизведения трека до {minProgress} мс");
    }

    private async Task HandlePauseAsync(long chatId, Guid? currentSessionId, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Сначала нужно войти в комнату для совместного прослушивания");
            return;
        }

        var spotifyClient = spotifyClientStorage.TryRead(chatId);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Сначала нужно пройти авторизацию в Spotify");
            return;
        }

        await ApplyToAllParticipants(currentSessionId.Value, client => client.Player.PausePlayback());
        await NotifyAllAsync(currentSessionId.Value, $"{username} ставит воспроизведение на паузу");
    }

    private async Task HandleUnpauseAsync(long chatId, Guid? currentSessionId, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Сначала нужно войти в комнату для совместного прослушивания");
            return;
        }

        var spotifyClient = spotifyClientStorage.TryRead(chatId);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Сначала нужно пройти авторизацию в Spotify");
            return;
        }

        await ApplyToAllParticipants(currentSessionId.Value, client => client.Player.ResumePlayback());
        await NotifyAllAsync(currentSessionId.Value, $"{username} возобновляет воспроизведение");
    }

    private async Task HandleNextTrackAsync(long chatId, Guid? currentSessionId, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Сначала нужно войти в комнату для совместного прослушивания");
            return;
        }

        var spotifyClient = spotifyClientStorage.TryRead(chatId);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Сначала нужно пройти авторизацию в Spotify");
            return;
        }

        await ApplyToAllParticipants(currentSessionId.Value, client => client.Player.SkipNext());
        await NotifyAllAsync(currentSessionId.Value, $"{username} переключает воспроизведение на следующий трек в очереди");
    }

    private ISpotifyClient[] GetAllParticipantClients(Guid currentSessionId)
    {
        var session = sessionsService.TryRead(currentSessionId)!;
        return session.Participants
                      .Select(userId => spotifyClientStorage.TryRead(userId))
                      .Where(client => client is not null)
                      .Select(client => client!)
                      .ToArray();
    }

    private async Task ApplyToAllParticipants(Guid currentSessionId, Func<ISpotifyClient, Task> action)
    {
        var clients = GetAllParticipantClients(currentSessionId);
        await Task.WhenAll(clients.Select(action));
    }

    private async Task StartSpotifyAuthAsync(long chatId)
    {
        var spotifyClient = spotifyClientFactory.CreateOrGet(chatId);
        var spotifyUser = await spotifyClient.UserProfile.Current();
        await SendResponseAsync(chatId, $"Успешная авторизация в Spotify как {spotifyUser.DisplayName}");
    }

    private async Task NotifyAllAsync(Guid sessionId, string message)
    {
        var session = sessionsService.TryRead(sessionId)!;
        await Task.WhenAll(
            session.Participants.Select(
                userId => SendResponseAsync(userId, message)
            )
        );
    }

    private async Task SendResponseAsync(long chatId, string message, ParseMode? parseMode = null)
    {
        if (parseMode is null)
        {
            await telegramBotClient.SendTextMessageAsync(chatId, message);
            return;
        }

        await telegramBotClient.SendTextMessageAsync(chatId, message, parseMode: parseMode);
    }

    private readonly ISessionsService sessionsService;
    private readonly ISpotifyClientFactory spotifyClientFactory;
    private readonly ISpotifyClientStorage spotifyClientStorage;
    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
    private readonly ITelegramBotClient telegramBotClient;
}