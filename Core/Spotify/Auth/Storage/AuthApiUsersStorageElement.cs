using Microsoft.EntityFrameworkCore;
using SqlRepositoryBase.Core.Models;

namespace Core.Spotify.Auth.Storage;

[Index(nameof(TelegramUserId))]
public class AuthApiUsersStorageElement : SqlStorageElement
{
    public long TelegramUserId { get; set; }
}