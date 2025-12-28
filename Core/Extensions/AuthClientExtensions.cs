using SpotifyHelpers.Api.Client.Auth;
using SpotifyHelpers.Dto.Auth;
using SpotifyHelpers.Dto.Exceptions;

namespace Core.Extensions;

public static class AuthClientExtensions
{
    public static async Task<SpotifyAuthorizationCodeDto?> TryGetAsync(this IAuthClient client, Guid userId)
    {
        try
        {
            return await client.GetAsync(userId);
        }
        catch (TokenNotFoundException e)
        {
            return null;
        }
    }
}