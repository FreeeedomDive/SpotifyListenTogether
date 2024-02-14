using Core.Settings;
using Core.Spotify.Auth;
using Core.Spotify.Auth.Storage;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using Telegram.Bot;

namespace Core.Spotify.Client;

public class SpotifyClientFactory : ISpotifyClientFactory
{
    public SpotifyClientFactory(
        ISpotifyClientStorage spotifyClientStorage,
        ITelegramBotClient telegramBotClient,
        ITokensRepository tokensRepository,
        IOptions<SpotifySettings> spotifySettings
    )
    {
        this.spotifyClientStorage = spotifyClientStorage;
        this.telegramBotClient = telegramBotClient;
        this.tokensRepository = tokensRepository;
        this.spotifySettings = spotifySettings;
    }

    public async Task<ISpotifyClient?> CreateOrGetAsync(long telegramUserId, bool forceReAuth = false)
    {
        if (!forceReAuth)
        {
            var existingClient = spotifyClientStorage.TryRead(telegramUserId);
            if (existingClient is not null)
            {
                return existingClient;
            }

            var token = await tokensRepository.TryReadAsync(telegramUserId);
            if (token is not null)
            {
                return await CreateClientAsync(token);
            }
        }

        lock (locker)
        {
            var authProvider = new SpotifyAuthProvider(spotifySettings);
            var authLink = authProvider.CreateAuthLinkAsync().GetAwaiter().GetResult();
            telegramBotClient.SendTextMessageAsync(telegramUserId, $"Теперь нужно авторизоваться в Spotify по этой ссылке: {authLink}\n(ссылка активна минуту)")
                             .GetAwaiter().GetResult();
            var token = authProvider.WaitForTokenAsync().GetAwaiter().GetResult();
            if (token is null)
            {
                return null;
            }

            var client = CreateClientAsync(token).GetAwaiter().GetResult();
            spotifyClientStorage.CreateOrUpdate(telegramUserId, client);
            tokensRepository.CreateOrUpdateAsync(telegramUserId, token).GetAwaiter().GetResult();
            return client;
        }
    }

    private async Task<ISpotifyClient> CreateClientAsync(string token)
    {
        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, token, new Uri(spotifySettings.Value.RedirectUri)
            )
        );
        var config = SpotifyClientConfig
                     .CreateDefault()
                     .WithAuthenticator(new AuthorizationCodeAuthenticator(spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, tokenResponse));

        return new SpotifyClient(config);
    }

    private readonly object locker = new();

    private readonly ISpotifyClientStorage spotifyClientStorage;
    private readonly IOptions<SpotifySettings> spotifySettings;
    private readonly ITelegramBotClient telegramBotClient;
    private readonly ITokensRepository tokensRepository;
}