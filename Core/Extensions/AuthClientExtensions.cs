using SpotifyAuth.Api.Client.Auth;
using SpotifyAuth.Dto;
using SpotifyAuth.Dto.Exceptions;

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