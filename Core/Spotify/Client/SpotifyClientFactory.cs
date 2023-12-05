using Core.Settings;
using Core.Spotify.Auth;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace Core.Spotify.Client;

public class SpotifyClientFactory : ISpotifyClientFactory
{
    public SpotifyClientFactory(
        ISpotifyClientStorage spotifyClientStorage,
        ITelegramBotClient telegramBotClient,
        IOptions<SpotifySettings> spotifySettings
    )
    {
        this.spotifyClientStorage = spotifyClientStorage;
        this.telegramBotClient = telegramBotClient;
        this.spotifySettings = spotifySettings;
    }

    public ISpotifyClient CreateOrGet(long telegramUserId, bool forceReAuth = false)
    {
        var existingClient = spotifyClientStorage.TryRead(telegramUserId);
        if (existingClient is not null && !forceReAuth)
        {
            return existingClient;
        }

        lock (locker)
        {
            var authProvider = new SpotifyAuthProvider(spotifySettings);
            var authLink = authProvider.CreateAuthLinkAsync().GetAwaiter().GetResult();
            telegramBotClient.SendTextMessageAsync(telegramUserId, $"Теперь нужно авторизоваться в Spotify по этой ссылке: {authLink}").GetAwaiter().GetResult();
            var client = authProvider.WaitForClientInitializationAsync().GetAwaiter().GetResult();
            spotifyClientStorage.CreateOrUpdate(telegramUserId, client);
            return client;
        }
    }

    private readonly object locker = new();

    private readonly ISpotifyClientStorage spotifyClientStorage;
    private readonly IOptions<SpotifySettings> spotifySettings;
    private readonly ITelegramBotClient telegramBotClient;
}