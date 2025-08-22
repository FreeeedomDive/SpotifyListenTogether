using Core.Commands.Base;
using Core.Extensions;
using Core.Sessions;
using Core.Settings;
using Core.Spotify.Auth.Storage;
using Core.Spotify.Client;
using Core.Whitelist;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SqlRepositoryBase.Core.ContextBuilders;
using Telegram.Bot;

namespace Core.Commands.Dev;

public class MigrationCommand : CommandBase, IMigrationCommand
{
    public MigrationCommand(
        IDbContextFactory dbContextFactory,
        IOptions<SpotifySettings> spotifySettings,
        ITokensService tokensService,
        ITelegramBotClient telegramBotClient,
        ISessionsService sessionsService,
        ISpotifyClientStorage spotifyClientStorage,
        ISpotifyClientFactory spotifyClientFactory,
        IWhitelistService whitelistService,
        ILogger<MigrationCommand> logger
    ) : base(telegramBotClient, sessionsService, spotifyClientStorage, spotifyClientFactory, whitelistService, logger)
    {
        this.dbContextFactory = dbContextFactory;
        this.spotifySettings = spotifySettings;
        this.tokensService = tokensService;
    }

    protected override async Task ExecuteAsync()
    {
        var databaseContext = dbContextFactory.Build();
        var tokens = await databaseContext.Set<TokenStorageElement>().ToArrayAsync();
        foreach (var tokenStorageElement in tokens)
        {
            try
            {
                var token = JsonConvert.DeserializeObject<AuthorizationCodeTokenResponse>(tokenStorageElement.Token)!;
                var client = CreateClient(token);
                var spotifyUser = await client.UserProfile.Current();
                var newId = await tokensService.CreateOrUpdateAsync(tokenStorageElement.UserId, token);
                Logger.LogInformation("User {userName} -> id {id}", spotifyUser.DisplayName, newId);
                await SendResponseAsync(UserId, $"User {spotifyUser.DisplayName} -> id {newId}");
            }
            catch (Exception e)
            {
                Logger.LogWarning(e, "Failed to restore spotify client for user {userId}", tokenStorageElement.UserId);
                await SendResponseAsync(UserId, $"Failed to restore spotify client for user {tokenStorageElement.UserId}, check logs");
            }
        }
        await SendResponseAsync(UserId, "Migration completed");
    }

    private SpotifyClient CreateClient(AuthorizationCodeTokenResponse token)
    {
        var config = SpotifyClientConfig
                     .CreateDefault()
                     .WithJSONSerializer(new JsonSerializerDecorator())
                     .WithAuthenticator(new AuthorizationCodeAuthenticator(spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, token));

        return new SpotifyClient(config);
    }

    private readonly IDbContextFactory dbContextFactory;
    private readonly IOptions<SpotifySettings> spotifySettings;
    private readonly ITokensService tokensService;
}