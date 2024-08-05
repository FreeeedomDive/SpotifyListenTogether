using Core.Extensions;
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

    public async Task InitializeAllSavedClientsAsync()
    {
        var savedTokens = await tokensRepository.ReadAllAsync();
        foreach (var (userId, token) in savedTokens)
        {
            var savedClient = CreateClient(token);
            spotifyClientStorage.CreateOrUpdate(userId, savedClient);
        }
    }

    public async Task<ISpotifyClient?> GetAsync(long telegramUserId)
    {
        var existingClient = spotifyClientStorage.TryRead(telegramUserId);
        if (existingClient is not null)
        {
            return existingClient;
        }

        var restoredClient = await RestoreClientAsync(telegramUserId);
        if (restoredClient is not null)
        {
            spotifyClientStorage.CreateOrUpdate(telegramUserId, restoredClient);
        }
        return restoredClient;
    }

    public async Task<ISpotifyClient?> CreateOrGetAsync(long telegramUserId, bool forceReAuth = false)
    {
        if (!forceReAuth)
        {
            var existingClient = await GetAsync(telegramUserId);
            if (existingClient is not null)
            {
                return existingClient;
            }
        }

        string? token;
        lock (locker)
        {
            var authProvider = new SpotifyAuthProvider(spotifySettings);
            var authLink = authProvider.CreateAuthLinkAsync().GetAwaiter().GetResult();
            telegramBotClient.SendTextMessageAsync(
                telegramUserId,
                $"Теперь нужно авторизоваться в Spotify по этой ссылке: {authLink}\n(ссылка активна минуту)"
            ).GetAwaiter().GetResult();
            token = authProvider.WaitForTokenAsync().GetAwaiter().GetResult();
            if (token is null)
            {
                return null;
            }
        }

        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, token, new Uri(spotifySettings.Value.RedirectUri)
            )
        );
        var client = CreateClient(tokenResponse);
        spotifyClientStorage.CreateOrUpdate(telegramUserId, client);
        await tokensRepository.CreateOrUpdateAsync(telegramUserId, tokenResponse);
        return client;
    }

    private async Task<ISpotifyClient?> RestoreClientAsync(long telegramUserId)
    {
        var savedToken = await tokensRepository.TryReadAsync(telegramUserId);
        if (savedToken is null)
        {
            return null;
        }

        var savedClient = CreateClient(savedToken);
        spotifyClientStorage.CreateOrUpdate(telegramUserId, savedClient);
        return savedClient;
    }

    private ISpotifyClient CreateClient(AuthorizationCodeTokenResponse token)
    {
        var config = SpotifyClientConfig
                     .CreateDefault()
                     .WithJSONSerializer(new JsonSerializerDecorator())
                     .WithAuthenticator(new AuthorizationCodeAuthenticator(spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, token));

        return new SpotifyClient(config);
    }

    private readonly object locker = new();

    private readonly ISpotifyClientStorage spotifyClientStorage;
    private readonly IOptions<SpotifySettings> spotifySettings;
    private readonly ITelegramBotClient telegramBotClient;
    private readonly ITokensRepository tokensRepository;
}