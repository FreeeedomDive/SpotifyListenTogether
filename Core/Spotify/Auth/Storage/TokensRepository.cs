using Core.Extensions;
using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAuth.Api.Client;
using SpotifyAuth.Dto;
using SqlRepositoryBase.Core.Repository;

namespace Core.Spotify.Auth.Storage;

public class TokensRepository : ITokensRepository
{
    public TokensRepository(
        ISqlRepository<TokenStorageElement> sqlRepository,
        ISqlRepository<AuthApiUsersStorageElement> authApiUsersRepository,
        ISpotifyAuthApiClient spotifyAuthApiClient
    )
    {
        this.sqlRepository = sqlRepository;
        this.authApiUsersRepository = authApiUsersRepository;
        this.spotifyAuthApiClient = spotifyAuthApiClient;
    }

    public async Task<(long UserId, AuthorizationCodeTokenResponse token)[]> ReadAllAsync()
    {
        var userIds = await authApiUsersRepository.ReadAllAsync();
        var tokens = await spotifyAuthApiClient.Auth.GetAsync(userIds.Select(x => x.Id).ToArray());
        var result = userIds
                     .Select(x => new
                         {
                             TelegramUserId = x.TelegramUserId,
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

        var token = await spotifyAuthApiClient.Auth.TryGetAsync(apiUser.Id);
        return token is null ? null : ToSpotifyToken(token);
    }

    public async Task CreateOrUpdateAsync(long userId, AuthorizationCodeTokenResponse token)
    {
        var apiUser = (await authApiUsersRepository.FindAsync(x => x.TelegramUserId == userId)).FirstOrDefault()
                      ?? new AuthApiUsersStorageElement
                      {
                          TelegramUserId = userId,
                          Id = Guid.NewGuid(),
                      };

        await spotifyAuthApiClient.Auth.CreateOrUpdateAsync(ToDto(apiUser.Id, token));
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

    public static SpotifyAuthorizationCodeDto ToDto(Guid userId, AuthorizationCodeTokenResponse token)
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

    private readonly ISqlRepository<TokenStorageElement> sqlRepository;
    private readonly ISqlRepository<AuthApiUsersStorageElement> authApiUsersRepository;
    private readonly ISpotifyAuthApiClient spotifyAuthApiClient;
}