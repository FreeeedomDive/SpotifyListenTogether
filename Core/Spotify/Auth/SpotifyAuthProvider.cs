using Core.Settings;
using Microsoft.Extensions.Options;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

namespace Core.Spotify.Auth;

public class SpotifyAuthProvider : ISpotifyAuthProvider
{
    public SpotifyAuthProvider(
        IOptions<SpotifySettings> spotifySettings
    )
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

    public async Task<string?> WaitForTokenAsync()
    {
        while (!cancellationTokenSource.IsCancellationRequested && token is null)
        {
            await Task.Delay(500);
        }

        return token;
    }

    private async Task OnAuthorizationCodeReceived(object _, AuthorizationCodeResponse response)
    {
        await server.Stop();
        token = response.Code;
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

    private string? token;

    private const string LocalCallbackUrl = "http://localhost:5069/callback";
}