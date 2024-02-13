using Core.Settings;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Core.Spotify.Auth;

public class SpotifyAuthProvider : ISpotifyAuthProvider
{
    public SpotifyAuthProvider(IOptions<SpotifySettings> spotifySettings)
    {
        this.spotifySettings = spotifySettings;
    }

    public async Task<string> CreateAuthLinkAsync()
    {
        server = new EmbedIOAuthServer(new Uri(LocalCallbackUrl), 5069);
        cancellationTokenSource = new CancellationTokenSource();
        await server.Start();

        server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
        server.ErrorReceived += OnErrorReceived;
        Task.Run(
            async () =>
            {
                await Task.Delay(1000 * 60);
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    await server.Stop();
                    cancellationTokenSource.Cancel();
                }
            }
        );

        var request = new LoginRequest(new Uri(spotifySettings.Value.RedirectUri), spotifySettings.Value.ClientId, LoginRequest.ResponseType.Code)
        {
            Scope = new List<string> { Scopes.UserModifyPlaybackState, Scopes.UserReadPlaybackState },
        };
        return request.ToUri().AbsoluteUri;
    }

    public async Task<SpotifyClient?> WaitForClientInitializationAsync()
    {
        while (!cancellationTokenSource.IsCancellationRequested && spotifyClient is null)
        {
            await Task.Delay(500);
        }

        return spotifyClient;
    }

    private async Task OnAuthorizationCodeReceived(object _, AuthorizationCodeResponse response)
    {
        await server.Stop();

        var tokenResponse = await new OAuthClient().RequestToken(
            new AuthorizationCodeTokenRequest(
                spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, response.Code, new Uri(spotifySettings.Value.RedirectUri)
            )
        );
        var config = SpotifyClientConfig
                     .CreateDefault()
                     .WithAuthenticator(new AuthorizationCodeAuthenticator(spotifySettings.Value.ClientId, spotifySettings.Value.ClientSecret, tokenResponse));

        spotifyClient = new SpotifyClient(config);
        cancellationTokenSource.Cancel();
    }

    private async Task OnErrorReceived(object sender, string error, string? state)
    {
        Console.WriteLine($"Aborting authorization, error received: {error}");
        await server.Stop();
    }

    private readonly IOptions<SpotifySettings> spotifySettings;

    private EmbedIOAuthServer server;
    private CancellationTokenSource cancellationTokenSource;

    private SpotifyClient? spotifyClient;

    private const string LocalCallbackUrl = "http://localhost:5069/callback";
}