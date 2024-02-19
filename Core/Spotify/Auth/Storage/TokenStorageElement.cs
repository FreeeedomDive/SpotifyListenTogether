using Microsoft.EntityFrameworkCore;
using SqlRepositoryBase.Core.Models;

namespace Core.Spotify.Auth.Storage;

[Index(nameof(UserId))]
public class TokenStorageElement : SqlStorageElement
{
    public long UserId { get; set; }
    public string Token { get; set; }
}