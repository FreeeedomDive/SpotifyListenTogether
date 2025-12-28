using Core.Extensions;
using SpotifyAPI.Web;
using SpotifyHelpers.Api.Client;
using SpotifyHelpers.Dto.Auth;
using SqlRepositoryBase.Core.Repository;

namespace Core.Spotify.Auth.Storage;

public class TokensService(
    ISqlRepository<AuthApiUsersStorageElement> authApiUsersRepository,
    ISpotifyHelpersApiClient spotifyHelpersApiClient
) : ITokensService
{
    public async Task<(long UserId, AuthorizationCodeTokenResponse token)[]> ReadAllAsync()
    {
        var apiUsers = await authApiUsersRepository.ReadAllAsync();
        if (apiUsers.Length == 0)
        {
            return Array.Empty<(long UserId, AuthorizationCodeTokenResponse token)>();
        }

        var ids = apiUsers.Select(x => x.Id).ToArray();
        var tokens = await spotifyHelpersApiClient.Auth.GetAsync(new SearchTokensDto
        {
            Ids = ids,
        });
        var result = apiUsers
                     .Select(x => new
                         {
                             x.TelegramUserId,
                             Token = tokens.Items.FirstOrDefault(t => t?.Id == x.Id),
                         }
                     )
                     .Where(x => x.Token is not null)
                     .Select(x => (x.TelegramUserId, ToSpotifyToken(x.Token!)))
                     .ToArray();

        return result;
    }

    public async Task<AuthorizationCodeTokenResponse?> TryReadAsync(long userId)
    {
        var apiUser = (await authApiUsersRepository.FindAsync(x => x.TelegramUserId == userId)).FirstOrDefault();
        if (apiUser is null)
        {
            return null;
        }

        var token = await spotifyHelpersApiClient.Auth.TryGetAsync(apiUser.Id);
        return token is null ? null : ToSpotifyToken(token);
    }

    public async Task<Guid> CreateOrUpdateAsync(long userId, AuthorizationCodeTokenResponse token)
    {
        var userStorageElement = (await authApiUsersRepository.FindAsync(x => x.TelegramUserId == userId)).FirstOrDefault();
        if (userStorageElement is null)
        {
            userStorageElement = new AuthApiUsersStorageElement
            {
                TelegramUserId = userId,
                Id = Guid.NewGuid(),
            };
            await authApiUsersRepository.CreateAsync(userStorageElement);
        }

        await spotifyHelpersApiClient.Auth.CreateOrUpdateAsync(ToDto(userStorageElement.Id, token));

        return userStorageElement.Id;
    }

    private static AuthorizationCodeTokenResponse ToSpotifyToken(SpotifyAuthorizationCodeDto dto)
    {
        return new AuthorizationCodeTokenResponse
        {
            AccessToken = dto.AccessToken,
            RefreshToken = dto.RefreshToken,
            ExpiresIn = dto.ExpiresIn,
            TokenType = dto.TokenType,
            Scope = dto.Scope,
            CreatedAt = dto.CreatedAt,
        };
    }

    private static SpotifyAuthorizationCodeDto ToDto(Guid userId, AuthorizationCodeTokenResponse token)
    {
        return new SpotifyAuthorizationCodeDto
        {
            Id = userId,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresIn = token.ExpiresIn,
            TokenType = token.TokenType,
            Scope = token.Scope,
            CreatedAt = token.CreatedAt,
        };
    }
}