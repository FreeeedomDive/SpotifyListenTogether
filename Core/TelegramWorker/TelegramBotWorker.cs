using Core.Extensions;
using Core.Sessions;
using Core.Spotify.Client;
using Core.Spotify.Links;
using SpotifyAPI.Web;
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
        ISessionsService sessionsService,
        ISpotifyClientFactory spotifyClientFactory,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyLinksRecognizeService spotifyLinksRecognizeService,
        ILoggerClient loggerClient
    )
    {
        this.telegramBotClient = telegramBotClient;
        this.sessionsService = sessionsService;
        this.spotifyClientFactory = spotifyClientFactory;
        this.spotifyClientStorage = spotifyClientStorage;
        this.spotifyLinksRecognizeService = spotifyLinksRecognizeService;
        this.loggerClient = loggerClient;
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

    private async Task HandlePollingErrorAsync(ITelegramBotClient client, Exception exception, CancellationToken cancellationToken)
    {
        await loggerClient.ErrorAsync(exception, "Unhandled polling error");
    }

    private async Task HandleUpdateAsync(ITelegramBotClient client, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is not { Text: { } messageText } message)
        {
            return;
        }

        var chatId = message.Chat.Id;
        var currentSessionId = sessionsService.Find(chatId);
        var username = message.Chat.Username ?? $"{message.Chat.FirstName} {message.Chat.LastName}";
        await loggerClient.InfoAsync("{user}: {command}", username, messageText);
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
                    await HandleLeaveSessionAsync(chatId, currentSessionId, username);
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
                case "/auth":
                    await HandleAuthAsync(chatId);
                    break;
                case "/_session":
                    await HandleCurrentSessionInfoAsync(chatId, currentSessionId);
                    break;
                default:
                    await HandleMessageAsync(chatId, currentSessionId, messageText, username);
                    break;
            }
        }
        catch (Exception e)
        {
            await loggerClient.ErrorAsync(e, "Unexpected error in message handler");
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
            await SendResponseAsync(chatId, $"Сейчас ты находишься в комнате `{currentSessionId}`, новую комнату создать нельзя", ParseMode.MarkdownV2);
            return;
        }

        var newSessionId = sessionsService.Create(chatId);
        await SendResponseAsync(chatId, $"Создана комната `{newSessionId}`", ParseMode.MarkdownV2);
        // start spotify auth in background to not block telegram messages handler 
        Task.Run(() => StartSpotifyAuthAsync(chatId));
    }

    private async Task HandleLeaveSessionAsync(long chatId, Guid? currentSessionId, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Нет текущей активной комнаты");
            return;
        }

        sessionsService.Leave(currentSessionId.Value, chatId);
        var session = sessionsService.TryRead(currentSessionId.Value)!;
        await NotifyAllAsync(
            currentSessionId.Value,
            $"{username} выходит из комнаты\n"
            + $"В этой комнате {session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
        );
        await SendResponseAsync(chatId, $"Ты покинул комнату `{currentSessionId}`", ParseMode.MarkdownV2);
    }

    private async Task HandleMessageAsync(long chatId, Guid? currentSessionId, string messageText, string username)
    {
        if (!currentSessionId.HasValue)
        {
            await HandleJoinSessionAsync(chatId, messageText, username);
            return;
        }

        await HandleAddMusicInSessionAsync(chatId, currentSessionId, messageText, username);
    }

    private async Task HandleJoinSessionAsync(long chatId, string messageText, string username)
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
            await NotifyAllAsync(
                sessionIdToJoin,
                $"{username} присоединяется\n"
                + $"В этой комнате {session.Participants.Count.ToPluralizedString("слушатель", "слушателя", "слушателей")}"
            );
            // start spotify auth in background to not block telegram messages handler 
            Task.Run(() => StartSpotifyAuthAsync(chatId));
        }
        catch (SessionNotFoundException)
        {
            await SendResponseAsync(chatId, $"Комната с кодом `{sessionIdToJoin}` не найдена", ParseMode.MarkdownV2);
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
            var searchResponse = await spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, messageText));
            var track = searchResponse.Tracks.Items?.FirstOrDefault();
            if (track is null)
            {
                await SendResponseAsync(chatId, "Ничего не найдено");
                return;
            }

            await PlayTrackAsync(currentSessionId!.Value, track, username);
            return;
        }

        switch (spotifyLink.Type)
        {
            case SpotifyLinkType.Track:
                var track = await spotifyClient.Tracks.Get(spotifyLink.Id);
                await PlayTrackAsync(currentSessionId!.Value, track, username);
                break;
            case SpotifyLinkType.Artist:
                await SendResponseAsync(chatId, "Воспроизведение исполнителей не поддерживается, советуем найти плейлист с этим исполнителем и воспроизвести его.");
                break;
            case SpotifyLinkType.Album:
                var album = await spotifyClient.Albums.Get(spotifyLink.Id);
                await PlayAlbumAsync(currentSessionId!.Value, album, username);
                break;
            case SpotifyLinkType.Playlist:
                var playlist = await spotifyClient.Playlists.Get(spotifyLink.Id);
                await PlayPlaylistAsync(currentSessionId!.Value, playlist, username);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private async Task PlayTrackAsync(Guid sessionId, FullTrack track, string username)
    {
        var session = sessionsService.TryRead(sessionId)!;
        var shouldAddToQueue = await ShouldAddToQueueAsync(sessionId);
        if (shouldAddToQueue)
        {
            await ApplyToAllParticipants(session.Id, (client, _) => client.Player.AddToQueue(new PlayerAddToQueueRequest(track.Uri)));
        }
        else
        {
            await ApplyToAllParticipants(
                session.Id, async (client, participant) =>
                {
                    await client.Player.ResumePlayback(
                        new PlayerResumePlaybackRequest
                        {
                            Uris = new List<string>
                            {
                                track.Uri,
                            },
                            DeviceId = participant.DeviceId,
                        }
                    );
                    SaveCurrentDeviceIdAsync(client, participant);
                }
            );
        }

        await NotifyAllAsync(
            session.Id,
            $"{username} добавляет в очередь {track.ToFormattedString()}", ParseMode.MarkdownV2
        );
    }

    private async Task<bool> ShouldAddToQueueAsync(Guid sessionId)
    {
        return (await Task.WhenAll(
                   GetAllParticipantSessionsAndClients(sessionId)
                       .Select(x => x.Value.SpotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest()))
               ))
               .Select(x => x?.Item is not FullTrack fullTrack ? null : fullTrack.Id)
               .Distinct()
               .Count() == 1;
    }

    private async Task PlayAlbumAsync(Guid sessionId, FullAlbum album, string username)
    {
        await ApplyToAllParticipants(
            sessionId, async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = album.Uri,
                        DeviceId = participant.DeviceId,
                    }
                );
                SaveCurrentDeviceIdAsync(client, participant);
            }
        );
        await NotifyAllAsync(
            sessionId,
            $"{username} начинает воспроизведение альбома {album.ToFormattedString()}",
            ParseMode.MarkdownV2
        );
        var session = sessionsService.TryRead(sessionId)!;
    }

    private async Task PlayPlaylistAsync(Guid sessionId, FullPlaylist playlist, string username)
    {
        await ApplyToAllParticipants(
            sessionId, async (client, participant) =>
            {
                await client.Player.SetShuffle(new PlayerShuffleRequest(false));
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        ContextUri = playlist.Uri,
                        DeviceId = participant.DeviceId,
                    }
                );
                SaveCurrentDeviceIdAsync(client, participant);
            }
        );
        await NotifyAllAsync(
            sessionId,
            $"{username} начинает воспроизведение плейлиста {playlist.ToFormattedString()}",
            ParseMode.MarkdownV2
        );
        var session = sessionsService.TryRead(sessionId)!;
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

        var clients = GetAllParticipantSessionsAndClients(currentSessionId.Value);
        var allCurrentProgress = await Task.WhenAll(
            clients.Values.Select(
                async x => (await x.SpotifyClient.Player.GetCurrentPlayback()).ProgressMs
            )
        );
        var minProgress = allCurrentProgress.Min();
        await ApplyToAllParticipants(
            currentSessionId.Value, async (client, _) =>
            {
                await client.Player.PausePlayback();
                await client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        PositionMs = minProgress,
                    }
                );
            }
        );
        await NotifyAllAsync(currentSessionId.Value, $"{username} сбрасывает прогресс воспроизведения трека до {minProgress} мс");
    }

    private async Task SaveCurrentDeviceIdAsync(ISpotifyClient spotifyClient, SessionParticipant participant, bool immediately = false)
    {
        try
        {
            if (!immediately)
            {
                await Task.Delay(5 * 1000);
            }

            var playback = await spotifyClient.Player.GetCurrentPlayback();
            participant.DeviceId = playback.Device.Id;
        }
        catch (Exception e)
        {
            await loggerClient.ErrorAsync(e, "Failed to save DeviceId");
        }
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

        await ApplyToAllParticipants(
            currentSessionId.Value, async (client, participant) =>
            {
                await client.Player.PausePlayback();
                await SaveCurrentDeviceIdAsync(client, participant, true);
            }
        );
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

        await ApplyToAllParticipants(
            currentSessionId.Value, (client, participant) =>
                client.Player.ResumePlayback(
                    new PlayerResumePlaybackRequest
                    {
                        DeviceId = participant.DeviceId,
                    }
                )
        );
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

        await ApplyToAllParticipants(currentSessionId.Value, (client, _) => client.Player.SkipNext());
        await NotifyAllAsync(currentSessionId.Value, $"{username} переключает воспроизведение на следующий трек в очереди");
    }

    private async Task HandleAuthAsync(long chatId)
    {
        Task.Run(() => StartSpotifyAuthAsync(chatId, true));
    }

    private async Task HandleCurrentSessionInfoAsync(long chatId, Guid? currentSessionId)
    {
        if (!currentSessionId.HasValue)
        {
            await SendResponseAsync(chatId, "Сначала нужно войти в комнату для совместного прослушивания");
            return;
        }

        var clientsByUserId = GetAllParticipantSessionsAndClients(currentSessionId.Value);
        var tasks = clientsByUserId.Select(
            async pair =>
            {
                var telegramId = pair.Key;
                var spotifyClient = pair.Value.SpotifyClient;

                var telegramName = (await telegramBotClient.GetChatAsync(telegramId)).Username!;
                var spotifyCurrentlyPlaying = await spotifyClient.Player.GetCurrentlyPlaying(new PlayerCurrentlyPlayingRequest());
                var spotifyCurrentlyPlayingTrack = spotifyCurrentlyPlaying?.Item as FullTrack;
                var currentPlayback = await spotifyClient.Player.GetCurrentPlayback();

                return $"*{telegramName}*\n" +
                       (spotifyCurrentlyPlaying is null || spotifyCurrentlyPlayingTrack is null
                           ? "No active devices found"
                           : $"Устройство: {currentPlayback.Device.Name.Escape()}\n"
                             + $"{spotifyCurrentlyPlayingTrack.ToFormattedString()}\n"
                             + $@"Прогресс: {TimeSpan.FromMilliseconds(currentPlayback.ProgressMs):m\:ss\.fff}".Escape());
            }
        );
        var playbackInfos = await Task.WhenAll(tasks);
        await SendResponseAsync(chatId, string.Join("\n\n", playbackInfos), ParseMode.MarkdownV2);
    }

    private Dictionary<long, (SessionParticipant Participant, ISpotifyClient SpotifyClient)> GetAllParticipantSessionsAndClients(Guid currentSessionId)
    {
        var session = sessionsService.TryRead(currentSessionId)!;
        return session.Participants
                      .Select(participant => (Participant: participant, SpotifyClient: spotifyClientStorage.TryRead(participant.UserId)))
                      .Where(pair => pair.SpotifyClient is not null)
                      .ToDictionary(pair => pair.Participant.UserId, pair => (pair.Participant, pair.SpotifyClient!));
    }

    private async Task ApplyToAllParticipants(Guid currentSessionId, Func<ISpotifyClient, SessionParticipant, Task> action)
    {
        var clients = GetAllParticipantSessionsAndClients(currentSessionId);
        await Task.WhenAll(clients.Values.Select(x => action(x.SpotifyClient, x.Participant)));
    }

    private async Task StartSpotifyAuthAsync(long chatId, bool forceReAuth = false)
    {
        var spotifyClient = spotifyClientFactory.CreateOrGet(chatId, forceReAuth);
        if (spotifyClient is null)
        {
            await SendResponseAsync(chatId, "Истекло время для авторизации");
            return;
        }

        var spotifyUser = await spotifyClient.UserProfile.Current();
        await SendResponseAsync(chatId, $"Успешная авторизация в Spotify как {spotifyUser.DisplayName}");
    }

    private async Task NotifyAllAsync(Guid sessionId, string message, ParseMode? parseMode = null)
    {
        var session = sessionsService.TryRead(sessionId)!;
        await Task.WhenAll(
            session.Participants.Select(
                participant => SendResponseAsync(participant.UserId, message, parseMode)
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

    private readonly ILoggerClient loggerClient;

    private readonly ISessionsService sessionsService;
    private readonly ISpotifyClientFactory spotifyClientFactory;
    private readonly ISpotifyClientStorage spotifyClientStorage;
    private readonly ISpotifyLinksRecognizeService spotifyLinksRecognizeService;
    private readonly ITelegramBotClient telegramBotClient;
}